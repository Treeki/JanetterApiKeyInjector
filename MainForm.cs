using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace JanetterApiKeyInjector
{
    public partial class MainForm : Form
    {
        Injector _injector = null;

        public MainForm()
        {
            InitializeComponent();
        }

        private void buttonLoad_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() != DialogResult.OK)
                return;

            using (var fs = new FileStream(openFileDialog1.FileName, FileMode.Open, FileAccess.Read))
            {
                if (checkBoxDebug.Checked)
                {
                    _injector = new Injector(fs);
                }
                else
                {
                    try
                    {
                        _injector = new Injector(fs);
                    }
                    catch (Exception exc)
                    {
                        MessageBox.Show(exc.Message, "Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }


                inputConsumerKey.Text = _injector.ConsumerKeys[0];
                inputConsumerSecret.Text = _injector.ConsumerSecrets[0];

                inputConsumerKey.Enabled = true;
                inputConsumerSecret.Enabled = true;
                buttonSave.Enabled = true;
            }
        }

        private void buttonSave_Click(object sender, EventArgs e)
        {
            if (saveFileDialog1.ShowDialog() != DialogResult.OK)
                return;

            for (int i = 0; i < _injector.ConsumerKeys.Length; i++)
            {
                _injector.ConsumerKeys[i] = inputConsumerKey.Text;
                _injector.ConsumerSecrets[i] = inputConsumerSecret.Text;
            }

            FileStream fs = null;

            try
            {
                fs = new FileStream(saveFileDialog1.FileName, FileMode.Create, FileAccess.ReadWrite);
            }
            catch (Exception exc)
            {
                MessageBox.Show(
                    "Could not open the file for writing. Are you trying to write inside Program Files without permission? This is the error returned:\r\n\r\n"
                    + exc.Message,
                    "Write Failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            try
            {
                _injector.Save(fs);
            }
            finally
            {
                fs.Dispose();
                fs = null;
            }

            MessageBox.Show("File successfully written. Enjoy!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
