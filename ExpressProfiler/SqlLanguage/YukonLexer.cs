using System;
using System.Drawing;
using System.Text;

namespace ExpressProfiler
{
    public class YukonLexer
    {
        #region Constants

        private const string HexDigits = "1234567890abcdefABCDEF";

        private const string IdentifierStr = "qwertyuiopasdfghjklzxcvbnmQWERTYUIOPASDFGHJKLZXCVBNM1234567890_#$";

        private const string NumberStr = "1234567890.-";

        #endregion

        #region Fields

        private readonly char[] m_IdentifiersArray = IdentifierStr.ToCharArray();

        private readonly Sqltokens m_Tokens = new Sqltokens();

        private string m_Line;

        private int m_Run;

        private int m_StringLen;

        private string m_Token = "";

        private TokenKind m_TokenId;

        private int m_TokenPos;

        #endregion

        #region Constructors and Destructors

        public YukonLexer()
        {
            Array.Sort(this.m_IdentifiersArray);
        }

        #endregion

        #region Enums

        public enum TokenKind
        {
            tkComment,

            tkDatatype,

            tkFunction,

            tkIdentifier,

            tkKey,

            tkNull,

            tkNumber,

            tkSpace,

            tkString,

            tkSymbol,

            tkUnknown,

            tkVariable,

            tkGreyKeyword,

            tkFuKeyword
        }

        private enum SqlRange
        {
            rsUnknown,

            rsComment,

            rsString
        }

        #endregion

        #region Properties

        private string Line
        {
            set
            {
                this.Range = SqlRange.rsUnknown;
                this.m_Line = value;
                this.m_Run = 0;
                this.Next();
            }
        }

        private SqlRange Range { get; set; }

        private string Token
        {
            get
            {
                /*int len = m_Run - m_TokenPos; return m_Line.Substring(m_TokenPos, len);*/
                return this.m_Token;
            }
        }

        private TokenKind TokenId
        {
            get
            {
                return this.m_TokenId;
            }
        }

        #endregion

        #region Public Methods and Operators

        public void FillRichEdit(System.Windows.Forms.RichTextBox rich, string value)
        {
            rich.Text = "";
            this.Line = value;

            var sb = new RTFBuilder { BackColor = rich.BackColor };

            while (this.TokenId != TokenKind.tkNull)
            {
                Color forecolor;
                switch (this.TokenId)
                {
                    case TokenKind.tkKey:
                        forecolor = Color.Blue;
                        break;
                    case TokenKind.tkFunction:
                        forecolor = Color.Fuchsia;
                        break;
                    case TokenKind.tkGreyKeyword:
                        forecolor = Color.Gray;
                        break;
                    case TokenKind.tkFuKeyword:
                        forecolor = Color.Fuchsia;
                        break;
                    case TokenKind.tkDatatype:
                        forecolor = Color.Blue;
                        break;
                    case TokenKind.tkNumber:
                        forecolor = Color.Red;
                        break;
                    case TokenKind.tkString:
                        forecolor = Color.Red;
                        break;
                    case TokenKind.tkComment:
                        forecolor = Color.DarkGreen;
                        break;
                    default:
                        forecolor = Color.Black;
                        break;
                }
                sb.ForeColor = forecolor;
                if (this.Token == Environment.NewLine || this.Token == "\r" || this.Token == "\n")
                {
                    sb.AppendLine();
                }
                else
                {
                    sb.Append(this.Token);
                }
                this.Next();
            }
            rich.Rtf = sb.ToString();
        }

        public string StandardSql(string sql)
        {
            var result = new StringBuilder();
            this.Line = sql;

            while (this.TokenId != TokenKind.tkNull)
            {
                switch (this.TokenId)
                {
                    case TokenKind.tkNumber:
                    case TokenKind.tkString:
                        result.Append("<??>");
                        break;
                    default:
                        result.Append(this.Token);
                        break;
                }
                this.Next();
            }

            return result.ToString();
        }

        #endregion

        #region Methods

        private void AndSymbolProc()
        {
            this.m_TokenId = TokenKind.tkSymbol;
            this.m_Run++;

            if (this.GetChar(this.m_Run) == '=' || this.GetChar(this.m_Run) == '&')
            {
                this.m_Run++;
            }
        }

        // ReSharper restore InconsistentNaming

        private void AnsiCProc()
        {
            switch (this.GetChar(this.m_Run))
            {
                case '\x00':
                    this.NullProc();
                    break;
                case '\x0A':
                    this.LFProc();
                    break;
                case '\x0D':
                    this.CRProc();
                    break;

                default:
                {
                    this.m_TokenId = TokenKind.tkComment;
                    char c;
                    do
                    {
                        if (this.GetChar(this.m_Run) == '*' && this.GetChar(this.m_Run + 1) == '/')
                        {
                            this.Range = SqlRange.rsUnknown;
                            this.m_Run += 2;
                            break;
                        }
                        this.m_Run++;
                        c = this.GetChar(this.m_Run);
                    }
                    while (!(c == '\x00' || c == '\x0A' || c == '\x0D'));
                    break;
                }
            }
        }

        private void AsciiCharProc()
        {
            if (this.GetChar(this.m_Run) == '\x00')
            {
                this.NullProc();
            }
            else
            {
                this.m_TokenId = TokenKind.tkString;
                if (this.m_Run > 0 || this.Range != SqlRange.rsString || this.GetChar(this.m_Run) != '\x27')
                {
                    this.Range = SqlRange.rsString;
                    char c;

                    do
                    {
                        this.m_Run++;
                        c = this.GetChar(this.m_Run);
                    }
                    while (!(c == '\x00' || c == '\x0A' || c == '\x0D' || c == '\x27'));

                    if (this.GetChar(this.m_Run) == '\x27')
                    {
                        this.m_Run++;
                        this.Range = SqlRange.rsUnknown;
                    }
                }
            }
        }

        private void BracketProc()
        {
            this.m_TokenId = TokenKind.tkIdentifier;
            this.m_Run++;

            while (
                !(this.GetChar(this.m_Run) == '\x00' || this.GetChar(this.m_Run) == '\x0A'
                  || this.GetChar(this.m_Run) == '\x0D'))
            {
                if (this.GetChar(this.m_Run) == ']')
                {
                    this.m_Run++;
                    break;
                }
                this.m_Run++;
            }
        }

        private void CRProc()
        {
            this.m_TokenId = TokenKind.tkSpace;
            this.m_Run++;
            if (this.GetChar(this.m_Run) == '\x0A')
            {
                this.m_Run++;
            }
        }

        private void DoInsideProc(char chr)
        {
            if ((chr >= 'A' && chr <= 'Z') || (chr >= 'a' && chr <= 'z') || (chr == '_') || (chr == '#'))
            {
                this.IdentProc();
                return;
            }

            if (chr >= '0' && chr <= '9')
            {
                this.NumberProc();
                return;
            }

            if ((chr >= '\x00' && chr <= '\x09') || (chr >= '\x0B' && chr <= '\x0C') || (chr >= '\x0E' && chr <= '\x20'))
            {
                this.SpaceProc();
                return;
            }

            this.UnknownProc();
        }

        private void DoProcTable(char chr)
        {
            switch (chr)
            {
                case '\x00':
                    this.NullProc();
                    break;
                case '\x0A':
                    this.LFProc();
                    break;
                case '\x0D':
                    this.CRProc();
                    break;
                case '\x27':
                    this.AsciiCharProc();
                    break;
                case '=':
                    this.EqualProc();
                    break;
                case '>':
                    this.GreaterProc();
                    break;
                case '<':
                    this.LowerProc();
                    break;
                case '-':
                    this.MinusProc();
                    break;
                case '|':
                    this.OrSymbolProc();
                    break;
                case '+':
                    this.PlusProc();
                    break;
                case '/':
                    this.SlashProc();
                    break;
                case '&':
                    this.AndSymbolProc();
                    break;
                case '\x22':
                    this.QuoteProc();
                    break;
                case ':':
                case '@':
                    this.VariableProc();
                    break;
                case '^':
                case '%':
                case '*':
                case '!':
                    this.SymbolAssignProc();
                    break;
                case '{':
                case '}':
                case '.':
                case ',':
                case ';':
                case '?':
                case '(':
                case ')':
                case ']':
                case '~':
                    this.SymbolProc();
                    break;
                case '[':
                    this.BracketProc();
                    break;
                default:
                    this.DoInsideProc(chr);
                    break;
            }
        }

        private void EqualProc()
        {
            this.m_TokenId = TokenKind.tkSymbol;
            this.m_Run++;

            if (this.GetChar(this.m_Run) == '=' || this.GetChar(this.m_Run) == '>')
            {
                this.m_Run++;
            }
        }

        private char GetChar(int idx)
        {
            return idx >= this.m_Line.Length ? '\x00' : this.m_Line[idx];
        }

        private void GreaterProc()
        {
            this.m_TokenId = TokenKind.tkSymbol;
            this.m_Run++;

            if (this.GetChar(this.m_Run) == '=' || this.GetChar(this.m_Run) == '>')
            {
                this.m_Run++;
            }
        }

        private TokenKind IdentKind()
        {
            this.KeyHash(this.m_Run);

            return
                this.m_Tokens[this.m_Line.Substring(this.m_TokenPos, this.m_Run + this.m_StringLen - this.m_TokenPos)];
        }

        private void IdentProc()
        {
            this.m_TokenId = this.IdentKind();
            this.m_Run += this.m_StringLen;

            if (this.m_TokenId == TokenKind.tkComment)
            {
                while (
                    !(this.GetChar(this.m_Run) == '\x00' || this.GetChar(this.m_Run) == '\x0A'
                      || this.GetChar(this.m_Run) == '\x0D'))
                {
                    this.m_Run++;
                }
            }
            else
            {
                while (IdentifierStr.IndexOf(this.GetChar(this.m_Run)) != -1)
                {
                    this.m_Run++;
                }
            }
        }

        private void KeyHash(int pos)
        {
            this.m_StringLen = 0;

            while (Array.BinarySearch(this.m_IdentifiersArray, this.GetChar(pos)) >= 0)
            {
                this.m_StringLen++;
                pos++;
            }
        }

        private void LFProc()
        {
            this.m_TokenId = TokenKind.tkSpace;
            this.m_Run++;
        }

        private void LowerProc()
        {
            this.m_TokenId = TokenKind.tkSymbol;
            this.m_Run++;

            switch (this.GetChar(this.m_Run))
            {
                case '=':
                    this.m_Run++;
                    break;
                case '<':
                    this.m_Run++;
                    if (this.GetChar(this.m_Run) == '=')
                    {
                        this.m_Run++;
                    }
                    break;
            }
        }

        private void MinusProc()
        {
            this.m_Run++;

            if (this.GetChar(this.m_Run) == '-')
            {
                this.m_TokenId = TokenKind.tkComment;
                char c;

                do
                {
                    this.m_Run++;
                    c = this.GetChar(this.m_Run);
                }
                while (!(c == '\x00' || c == '\x0A' || c == '\x0D'));
            }
            else
            {
                this.m_TokenId = TokenKind.tkSymbol;
            }
        }

        private void Next()
        {
            this.m_TokenPos = this.m_Run;

            switch (this.Range)
            {
                case SqlRange.rsComment:
                    this.AnsiCProc();
                    break;
                case SqlRange.rsString:
                    this.AsciiCharProc();
                    break;
                default:
                    this.DoProcTable(this.GetChar(this.m_Run));
                    break;
            }

            this.m_Token = this.m_Line.Substring(this.m_TokenPos, this.m_Run - this.m_TokenPos);
        }

        private void NullProc()
        {
            this.m_TokenId = TokenKind.tkNull;
        }

        private void NumberProc()
        {
            this.m_TokenId = TokenKind.tkNumber;

            if (this.GetChar(this.m_Run) == '0'
                && (this.GetChar(this.m_Run + 1) == 'X' || this.GetChar(this.m_Run + 1) == 'x'))
            {
                this.m_Run += 2;
                while (HexDigits.IndexOf(this.GetChar(this.m_Run)) != -1)
                {
                    this.m_Run++;
                }
                return;
            }

            this.m_Run++;
            this.m_TokenId = TokenKind.tkNumber;

            while (NumberStr.IndexOf(this.GetChar(this.m_Run)) != -1)
            {
                if (this.GetChar(this.m_Run) == '.' && this.GetChar(this.m_Run + 1) == '.')
                {
                    break;
                }
                this.m_Run++;
            }
        }

        private void OrSymbolProc()
        {
            this.m_TokenId = TokenKind.tkSymbol;
            this.m_Run++;

            if (this.GetChar(this.m_Run) == '=' || this.GetChar(this.m_Run) == '|')
            {
                this.m_Run++;
            }
        }

        private void PlusProc()
        {
            this.m_TokenId = TokenKind.tkSymbol;
            this.m_Run++;

            if (this.GetChar(this.m_Run) == '=' || this.GetChar(this.m_Run) == '=')
            {
                this.m_Run++;
            }
        }

        private void QuoteProc()
        {
            this.m_TokenId = TokenKind.tkIdentifier;
            this.m_Run++;

            while (
                !(this.GetChar(this.m_Run) == '\x00' || this.GetChar(this.m_Run) == '\x0A'
                  || this.GetChar(this.m_Run) == '\x0D'))
            {
                if (this.GetChar(this.m_Run) == '\x22')
                {
                    this.m_Run++;
                    break;
                }
                this.m_Run++;
            }
        }

        private void SlashProc()
        {
            this.m_Run++;

            switch (this.GetChar(this.m_Run))
            {
                case '*':
                {
                    this.Range = SqlRange.rsComment;
                    this.m_TokenId = TokenKind.tkComment;

                    do
                    {
                        this.m_Run++;

                        if (this.GetChar(this.m_Run) == '*' && this.GetChar(this.m_Run + 1) == '/')
                        {
                            this.Range = SqlRange.rsUnknown;
                            this.m_Run += 2;
                            break;
                        }
                    }
                    while (
                        !(this.GetChar(this.m_Run) == '\x00' || this.GetChar(this.m_Run) == '\x0D'
                          || this.GetChar(this.m_Run) == '\x0A'));
                }
                    break;
                case '=':
                    this.m_Run++;
                    this.m_TokenId = TokenKind.tkSymbol;
                    break;
                default:
                    this.m_TokenId = TokenKind.tkSymbol;
                    break;
            }
        }

        private void SpaceProc()
        {
            this.m_TokenId = TokenKind.tkSpace;
            char c;

            do
            {
                this.m_Run++;
                c = this.GetChar(this.m_Run);
            }
            while (!(c > '\x20' || c == '\x00' || c == '\x0A' || c == '\x0D'));
        }

        private void SymbolAssignProc()
        {
            this.m_TokenId = TokenKind.tkSymbol;
            this.m_Run++;

            if (this.GetChar(this.m_Run) == '=')
            {
                this.m_Run++;
            }
        }

        private void SymbolProc()
        {
            this.m_Run++;
            this.m_TokenId = TokenKind.tkSymbol;
        }

        private void UnknownProc()
        {
            this.m_Run++;
            this.m_TokenId = TokenKind.tkUnknown;
        }

        private void VariableProc()
        {
            if (this.GetChar(this.m_Run) == '@' && this.GetChar(this.m_Run + 1) == '@')
            {
                this.m_Run += 2;
                this.IdentProc();
            }
            else
            {
                this.m_TokenId = TokenKind.tkVariable;
                int i = this.m_Run;

                do
                {
                    i++;
                }
                while (IdentifierStr.IndexOf(this.GetChar(i)) != -1);

                this.m_Run = i;
            }
        }

        #endregion
    }
}