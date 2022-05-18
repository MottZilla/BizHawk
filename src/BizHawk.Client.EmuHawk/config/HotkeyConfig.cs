﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class HotkeyConfig : Form
	{
		private readonly Config _config;

		public HotkeyConfig(Config config)
		{
			_config = config;
			InitializeComponent();
			Icon = Properties.Resources.HotKeysIcon;
			tabPage1.Focus();
		}

		protected override void OnActivated(EventArgs e)
		{
			base.OnActivated(e);
			Input.Instance.ControlInputFocus(this, ClientInputFocus.Mouse, true);
		}

		protected override void OnDeactivate(EventArgs e)
		{
			base.OnDeactivate(e);
			Input.Instance.ControlInputFocus(this, ClientInputFocus.Mouse, false);
		}

		private void HotkeyConfig_Load(object sender, EventArgs e)
		{
			var source = new AutoCompleteStringCollection();
			source.AddRange(HotkeyInfo.AllHotkeys.Keys.ToArray());

			SearchBox.AutoCompleteCustomSource = source;
			SearchBox.AutoCompleteSource = AutoCompleteSource.CustomSource;

			AutoTabCheckBox.Checked = _config.HotkeyConfigAutoTab;
			DoTabs();
			DoFocus();
		}

		private void HotkeyConfig_FormClosed(object sender, FormClosedEventArgs e)
		{
			Input.Instance.ClearEvents();
		}

		private void IDB_CANCEL_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void IDB_SAVE_Click(object sender, EventArgs e)
		{
			Save();
			DialogResult = DialogResult.OK;
			Close();
		}

		private void AutoTabCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			SetAutoTab();
		}

		private void Save()
		{
			_config.HotkeyConfigAutoTab = AutoTabCheckBox.Checked;
			foreach (var w in InputWidgets) _config.HotkeyBindings[w.WidgetName] = w.Bindings;
		}

		private IEnumerable<InputCompositeWidget> InputWidgets =>
			HotkeyTabControl.TabPages.Cast<TabPage>().SelectMany(tp => tp.Controls.OfType<InputCompositeWidget>());

		private void DoTabs()
		{
			HotkeyTabControl.SuspendLayout();
			HotkeyTabControl.TabPages.Clear();

			foreach (var tab in HotkeyInfo.Groupings)
			{
				var tb = new TabPage { Name = tab, Text = tab };
				var bindings = HotkeyInfo.AllHotkeys.Where(kvp => kvp.Value.TabGroup == tab)
					.OrderBy(static kvp => kvp.Value.Ordinal).ThenBy(static kvp => kvp.Value.DisplayName);
				int x = UIHelper.ScaleX(6);
				int y = UIHelper.ScaleY(14);
				int iwOffsetX = UIHelper.ScaleX(110);
				int iwOffsetY = UIHelper.ScaleY(-4);
				int iwWidth = UIHelper.ScaleX(120);

				tb.SuspendLayout();

				foreach (var (k, b) in bindings)
				{
					var l = new Label
					{
						Text = b.DisplayName,
						Location = new Point(x, y),
						Size = new Size(iwOffsetX - UIHelper.ScaleX(2), UIHelper.ScaleY(15))
					};

					var w = new InputCompositeWidget(_config.ModifierKeysEffective)
					{
						Location = new Point(x + iwOffsetX, y + iwOffsetY),
						AutoTab = AutoTabCheckBox.Checked,
						Width = iwWidth,
						WidgetName = k
					};

					w.SetupTooltip(toolTip1, b.ToolTip);
					toolTip1.SetToolTip(l, b.ToolTip);

					w.Bindings = _config.HotkeyBindings[k];

					tb.Controls.Add(l);
					tb.Controls.Add(w);

					y += UIHelper.ScaleY(24);
					if (y > HotkeyTabControl.Height - UIHelper.ScaleY(35))
					{
						x += iwOffsetX + iwWidth + UIHelper.ScaleX(10);
						y = UIHelper.ScaleY(14);
					}
				}

				if (tab == "TAStudio")
				{
					tb.Controls.Add(new Label
					{
						Text = "Save States hotkeys operate with branches when TAStudio is engaged.",
						Location = new Point(x, y),
						Size = new Size(iwWidth + iwOffsetX, HotkeyTabControl.Height - y)
					});
				}

				HotkeyTabControl.TabPages.Add(tb);
				tb.ResumeLayout();
			}

			HotkeyTabControl.ResumeLayout();
		}

		private void Defaults()
		{
			foreach (var w in InputWidgets) w.Bindings = HotkeyInfo.AllHotkeys[w.WidgetName].DefaultBinding;
		}

		private void ClearAll(bool currentTabOnly)
		{
			if (currentTabOnly)
			{
				foreach (var w in InputWidgets)
				{
					w.Clear();
				}
			}
			else
			{
				foreach (var w in HotkeyTabControl.SelectedTab.Controls.OfType<InputCompositeWidget>())
				{
					w.Clear();
				}
			}
		}

		private void SetAutoTab()
		{
			foreach (var w in InputWidgets)
			{
				w.AutoTab = AutoTabCheckBox.Checked;
			}
		}

		private void HotkeyTabControl_SelectedIndexChanged(object sender, EventArgs e)
		{
			DoFocus();
		}

		private void DoFocus()
		{
			if (HotkeyTabControl.SelectedTab != null)
			{
				foreach (var c in HotkeyTabControl.SelectedTab.Controls.OfType<InputWidget>())
				{
					c.Focus();
					return;
				}
			}
		}

		private void SearchBox_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.IsPressed(Keys.Enter) || e.IsPressed(Keys.Tab))
			{
				var k = HotkeyInfo.AllHotkeys.FirstOrDefault(kvp => string.Compare(kvp.Value.DisplayName, SearchBox.Text, true) is 0).Key;

				// Found
				if (k is not null)
				{
					var w = InputWidgets.FirstOrDefault(x => x.WidgetName == k);
					if (w != null)
					{
						HotkeyTabControl.SelectTab((TabPage)w.Parent);
						w.Focus();
					}
				}

				e.Handled = true;
			}
		}

		private void ClearAllToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ClearAll(true);
		}

		private void ClearCurrentTabToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ClearAll(false);
		}

		private void RestoreDefaultsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Defaults();
		}
	}
}
