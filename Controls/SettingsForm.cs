using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace WordLight.Controls
{
    public partial class SettingsForm : Form
    {
        private WordLightSettings _settings;

        public SettingsForm(WordLightSettings settings)
        {
            if (settings == null) throw new ArgumentNullException("settigns");
            _settings = settings;

            InitializeComponent();

            markColorPreview.BackColor = _settings.SearchMarkOutlineColor;
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            _settings.SearchMarkOutlineColor = markColorPreview.BackColor;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnMarkColor_Click(object sender, EventArgs e)
        {
            using (ColorDialog dialog = new ColorDialog())
            {
                dialog.AllowFullOpen = true;
                dialog.Color = markColorPreview.BackColor;
                DialogResult result = dialog.ShowDialog(this);
                if (result == DialogResult.OK)
                {
                    markColorPreview.BackColor = dialog.Color;
                }
            }
        }
    }
}
