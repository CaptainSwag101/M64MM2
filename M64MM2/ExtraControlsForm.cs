﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using static M64MM2.Utils;

namespace M64MM2
{
    public partial class ExtraControlsForm : Form
    {
        public ExtraControlsForm()
        {
            InitializeComponent();
        }

        void tbLevitate_ValueChanged(object sender, EventArgs e)
        {
            byte[] data = { (byte) tbLevitate.Value };
            WriteBytes(BaseAddress + 0x33B223, data);
        }

        void btnRemoveHud_Click(object sender, EventArgs e)
        {
            WriteUInt(BaseAddress + 0x2E3DB0, 0);
            WriteUInt(BaseAddress + 0x2E3DE0, 0);
            WriteUInt(BaseAddress + 0x2E3E18, 0);
            WriteUInt(BaseAddress + 0x2E3DC8, 0);
            WriteUInt(BaseAddress + 0x3325F4, 0x1);
        }
    }
}