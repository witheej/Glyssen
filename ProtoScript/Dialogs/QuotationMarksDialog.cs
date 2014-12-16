﻿using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using L10NSharp;
using L10NSharp.UI;

namespace ProtoScript.Dialogs
{
	public partial class QuotationMarksDialog : Form
	{
		private readonly Project m_project;
		private string m_hintFormat;

		internal QuotationMarksDialog(Project project)
		{
			InitializeComponent();

			m_project = project;

			SetupQuoteMarksComboBoxes(m_project.QuoteSystem);

			LocalizeItemDlg.StringsLocalized += HandleStringsLocalized;
		}

		private void HandleStringsLocalized()
		{
			SetupQuoteMarksComboBoxes(CurrentQuoteSystem);
		}

		private void SetupQuoteMarksComboBoxes(QuoteSystem currentSystem)
		{
			m_comboQuoteMarks.Items.AddRange(QuoteSystem.AllUniqueFirstLevelSystems.ToArray());
			m_comboQuoteMarks.SelectedItem = currentSystem;

			var quotationDashMarker = currentSystem.QuotationDashMarker;
			m_chkDialogueQuotations.Checked = !String.IsNullOrEmpty(quotationDashMarker);
			m_cboQuotationDash.Items.Clear();
			m_cboQuotationDash.Items.Add(string.Format(LocalizationManager.GetString("QuotationMarksDialog.QuotationDash", "Quotation dash ({0})"), "U+2015"));
			m_cboQuotationDash.Items.Add(string.Format(LocalizationManager.GetString("QuotationMarksDialog.EmDash", "Em-dash ({0})"), "U+2014"));
			switch (quotationDashMarker)
			{
				case "\u2015": m_cboQuotationDash.SelectedIndex = 0; break;
				case "\u2014": m_cboQuotationDash.SelectedIndex = 1; break;
				default: m_cboQuotationDash.Text = quotationDashMarker; break;
			}

			m_cboEndQuotationDash.Items.Clear();
			m_cboEndQuotationDash.Items.Add(LocalizationManager.GetString("QuotationMarksDialog.EndQuotationDashWithParagraphOnly", "End of paragraph (only)"));
			m_cboEndQuotationDash.Items.Add(SameAsStartDashText);
			m_cboEndQuotationDash.Items.Add(LocalizationManager.GetString("QuotationMarksDialog.EndQuotationDashWithAnyPunctuation", "Any punctuation mark"));

			var quotationDashEndMarker = currentSystem.QuotationDashEndMarker;
			switch (quotationDashEndMarker)
			{
				case "\u2015": m_cboQuotationDash.SelectedIndex = 0; break;
				case "\u2014": m_cboQuotationDash.SelectedIndex = 1; break;
				default: m_cboQuotationDash.Text = quotationDashMarker; break;
			}
		}

		private string SameAsStartDashText
		{
			get
			{
				string quotationDashMarker;
				switch (m_cboQuotationDash.SelectedIndex)
				{
					case 0: quotationDashMarker = "U+2015"; break;
					case 1: quotationDashMarker = "U+2014"; break;
					default: quotationDashMarker = m_cboQuotationDash.Text; break;
				}

				if (String.IsNullOrWhiteSpace(quotationDashMarker))
					return LocalizationManager.GetString("QuotationMarksDialog.EndQuotationDashWithStartDash",
						"Same as start quotation dash");
					
				return string.Format(LocalizationManager.GetString("QuotationMarksDialog.EndQuotationDashWithStartDash",
					"Same as start quotation dash ({0})"), quotationDashMarker);
			}
		}

		private void HandlecomboQuoteMarksDrawItem(object sender, DrawItemEventArgs e)
		{
			e.DrawBackground();
			if (e.Index < 0)
				TextRenderer.DrawText(e.Graphics, string.Empty, m_comboQuoteMarks.Font, e.Bounds, m_comboQuoteMarks.ForeColor,
					TextFormatFlags.Left);
			else
			{
				var selectedQuoteSystem = (QuoteSystem)m_comboQuoteMarks.Items[e.Index];
				string text = selectedQuoteSystem.ToString();
				var color = ((e.State & DrawItemState.Selected) > 0) ? SystemColors.HighlightText : m_comboQuoteMarks.ForeColor;

				TextRenderer.DrawText(e.Graphics, text, m_comboQuoteMarks.Font, e.Bounds, color,
					TextFormatFlags.Left);
				var quotesWidth = TextRenderer.MeasureText(e.Graphics, text, m_comboQuoteMarks.Font).Width;

				string majorLanguage = LocalizationManager.GetDynamicString("ProtoscriptGenerator",
					"QuotationMarks.MajorLanguage" + selectedQuoteSystem.MajorLanguage, selectedQuoteSystem.MajorLanguage);

				text = string.Format(LocalizationManager.GetString("QuotationMarksDialog.QuoteUsageFormat", "(commonly used in {0})",
				"Parameter is the name of a language and/or country"), majorLanguage);

				var bounds = new Rectangle(e.Bounds.Left + quotesWidth, e.Bounds.Top, e.Bounds.Width - quotesWidth, e.Bounds.Height);

				TextRenderer.DrawText(e.Graphics, text, m_cboQuotationDash.Font, bounds, color,
					TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
			}

			if (e.State != DrawItemState.ComboBoxEdit)
				e.Graphics.DrawLine(Pens.Black, new Point(e.Bounds.Left, e.Bounds.Bottom - 1), new Point(e.Bounds.Right, e.Bounds.Bottom - 1));
		}

		private void m_btnOk_Click(object sender, EventArgs e)
		{
			if (m_project.ConfirmedQuoteSystem != null && m_project.ConfirmedQuoteSystem != CurrentQuoteSystem)
			{
				string msg = LocalizationManager.GetString("ProjectSettingsDialog.ConfirmReparseMessage", "Changing the quote system will require a reparse of the text. Are you sure?");
				string title = LocalizationManager.GetString("ProjectSettingsDialog.ConfirmReparse", "Confirm Reparse");
				if (MessageBox.Show(msg, title, MessageBoxButtons.YesNo) != DialogResult.Yes)
				{
					SetupQuoteMarksComboBoxes(m_project.QuoteSystem);
					return;
				}
			}
			m_project.QuoteSystem = CurrentQuoteSystem;
			DialogResult = DialogResult.OK;
			Close();
		}

		public QuoteSystem CurrentQuoteSystem
		{
			get
			{
				var quoteSystemForFirstLevelQuotes = (QuoteSystem)m_comboQuoteMarks.SelectedItem;
				string quotationDashMarker = null;
				string quotationDashEndMarker = null;
				bool quotationDashesIndicateChangeOfSpeakerInFirstLevelQuotes = false;
				if (m_chkDialogueQuotations.Checked)
				{
					switch (m_cboQuotationDash.SelectedIndex)
					{
						case 0: quotationDashMarker = "\u2015"; break;
						case 1: quotationDashMarker = "\u2014"; break;
						default:
							if (!String.IsNullOrWhiteSpace(m_cboQuotationDash.Text))
								quotationDashMarker = m_cboQuotationDash.Text;
							break;
					}

					switch (m_cboEndQuotationDash.SelectedIndex)
					{
						case 0: break;
						case 1: quotationDashEndMarker = quotationDashMarker; break;
						case 2: quotationDashEndMarker = QuoteSystem.AnyPunctuation; break;
						default:
							if (!String.IsNullOrWhiteSpace(m_cboEndQuotationDash.Text))
								quotationDashMarker = m_cboEndQuotationDash.Text;
							break;
					}

					quotationDashesIndicateChangeOfSpeakerInFirstLevelQuotes = m_chkAlternateSpeakersInFirstLevelQuotes.Checked;
				}
				return QuoteSystem.GetOrCreateQuoteSystem(quoteSystemForFirstLevelQuotes.StartQuoteMarker,
					quoteSystemForFirstLevelQuotes.EndQuoteMarker,
					quotationDashMarker, quotationDashEndMarker, quotationDashesIndicateChangeOfSpeakerInFirstLevelQuotes);
			}
		}

		private void m_chkDialogueQuotations_CheckedChanged(object sender, EventArgs e)
		{
			m_cboQuotationDash.Enabled = m_chkDialogueQuotations.Checked;
			m_cboEndQuotationDash.Enabled = m_chkDialogueQuotations.Checked;
			m_lblEndDialogueQuote.Enabled = m_chkDialogueQuotations.Checked;
			m_chkAlternateSpeakersInFirstLevelQuotes.Enabled = m_chkDialogueQuotations.Checked;
		}

		private void m_cboQuotationDash_TextChanged(object sender, EventArgs e)
		{
			m_cboEndQuotationDash.Items[1] = SameAsStartDashText;
		}
	}
}
