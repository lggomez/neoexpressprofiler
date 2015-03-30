using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing;
using System.Text;

namespace ExpressProfiler
{
    internal class RTFBuilder
    {
        #region Static Fields

        private static readonly char[] Slashable = new[] { '{', '}', '\\' };

        #endregion

        #region Fields

        private readonly List<Color> m_Colortable = new List<Color>();

        private readonly float m_DefaultFontSize;

        private readonly StringCollection m_Fonttable = new StringCollection();

        private readonly StringBuilder m_Sb = new StringBuilder();

        private Color m_Backcolor;

        private Color m_Forecolor;

        #endregion

        #region Constructors and Destructors

        public RTFBuilder()
        {
            this.ForeColor = Color.FromKnownColor(KnownColor.WindowText);
            this.BackColor = Color.FromKnownColor(KnownColor.Window);
            this.m_DefaultFontSize = 20F;
        }

        #endregion

        #region Public Properties

        public Color BackColor
        {
            set
            {
                if (!this.m_Colortable.Contains(value))
                {
                    this.m_Colortable.Add(value);
                }

                if (value != this.m_Backcolor)
                {
                    this.m_Sb.Append(String.Format("\\highlight{0} ", this.m_Colortable.IndexOf(value) + 1));
                }

                this.m_Backcolor = value;
            }
        }

        public Color ForeColor
        {
            set
            {
                if (!this.m_Colortable.Contains(value))
                {
                    this.m_Colortable.Add(value);
                }

                if (value != this.m_Forecolor)
                {
                    this.m_Sb.Append(String.Format("\\cf{0} ", this.m_Colortable.IndexOf(value) + 1));
                }

                this.m_Forecolor = value;
            }
        }

        #endregion

        #region Public Methods and Operators

        public void Append(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                value = CheckChar(value);

                if (value.IndexOf(Environment.NewLine, StringComparison.Ordinal) >= 0)
                {
                    string[] lines = value.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

                    foreach (string line in lines)
                    {
                        this.m_Sb.Append(line);
                        this.m_Sb.Append("\\line ");
                    }
                }
                else
                {
                    this.m_Sb.Append(value);
                }
            }
        }

        public void AppendLine()
        {
            this.m_Sb.AppendLine("\\line");
        }

        public new string ToString()
        {
            var result = new StringBuilder();
            result.Append("{\\rtf1\\ansi\\ansicpg1252\\deff0\\deflang3081");
            result.Append("{\\fonttbl");

            for (int i = 0; i < this.m_Fonttable.Count; i++)
            {
                try
                {
                    result.Append(string.Format(this.m_Fonttable[i], i));
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            result.AppendLine("}");
            result.Append("{\\colortbl ;");

            foreach (Color item in this.m_Colortable)
            {
                result.AppendFormat("\\red{0}\\green{1}\\blue{2};", item.R, item.G, item.B);
            }

            result.AppendLine("}");
            result.Append("\\viewkind4\\uc1\\pard\\plain\\f0");
            result.AppendFormat("\\fs{0} ", this.m_DefaultFontSize);
            result.AppendLine();
            result.Append(this.m_Sb.ToString());
            result.Append("}");

            return result.ToString();
        }

        #endregion

        #region Methods

        private static string CheckChar(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                if (value.IndexOfAny(Slashable) >= 0)
                {
                    value = value.Replace("{", "\\{").Replace("}", "\\}").Replace("\\", "\\\\");
                }

                bool replaceuni = false;

                for (int i = 0; i < value.Length; i++)
                {
                    if (value[i] > 255)
                    {
                        replaceuni = true;
                        break;
                    }
                }

                if (replaceuni)
                {
                    var sb = new StringBuilder();

                    for (int i = 0; i < value.Length; i++)
                    {
                        if (value[i] <= 255)
                        {
                            sb.Append(value[i]);
                        }
                        else
                        {
                            sb.Append("\\u");
                            sb.Append((int)value[i]);
                            sb.Append("?");
                        }
                    }

                    value = sb.ToString();
                }
            }

            return value;
        }

        #endregion
    }
}