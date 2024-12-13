using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace GameVM.Compiler.Python.ANTLR
{
    public abstract class PythonLexerBase : Lexer
    {
        private const string ERR_TXT = " ERROR: ";

        protected readonly LinkedList<IToken> _pendingTokens;
        protected IToken? _lastToken;
        protected int _indentCount;
        protected readonly Stack<int> _indentStack;
        protected bool _wasSpaceOrTab;
        protected bool _wasNewLine;
        protected bool _opened;
        protected bool _suppressNewlines;

        protected PythonLexerBase(ICharStream input)
            : base(input)
        {
            _pendingTokens = new LinkedList<IToken>();
            _indentStack = new Stack<int>();
            _indentStack.Push(0);
            _indentCount = 0;
            _wasSpaceOrTab = false;
            _wasNewLine = false;
            _opened = false;
            _suppressNewlines = false;
        }

        protected PythonLexerBase(ICharStream input, TextWriter output, TextWriter errorOutput) : base(input, output, errorOutput)
        {
            _pendingTokens = new LinkedList<IToken>();
            _indentStack = new Stack<int>();
            _indentStack.Push(0);
            _indentCount = 0;
            _wasSpaceOrTab = false;
            _wasNewLine = false;
            _opened = false;
            _suppressNewlines = false;
        }

        public override void Reset()
        {
            _pendingTokens.Clear();
            _indentStack.Clear();
            _indentStack.Push(0);
            _lastToken = null;
            _wasNewLine = false;
            _indentCount = 0;
            base.Reset();
        }

        public override IToken NextToken()
        {
            // Check if we have any pending tokens
            if (_pendingTokens.Count > 0)
            {
                var token = _pendingTokens.First!.Value;
                _pendingTokens.RemoveFirst();
                _lastToken = token;
                return token;
            }

            // Get the next token from the character stream
            var next = base.NextToken();

            // Handle end of file
            if (next.Type == IntStreamConstants.EOF)
            {
                if (_wasNewLine)
                {
                    _wasNewLine = false;
                    return HandleEndOfFile(next);
                }
                else
                {
                    // Add a NEWLINE token before EOF if there wasn't one
                    CreateAndAddPendingToken(PythonLexer.NEWLINE, next.Line, next.Column, "\n");
                    _wasNewLine = true;
                    return NextToken();
                }
            }

            // Update state based on current token
            if (next.Type == PythonLexer.NEWLINE)
            {
                _wasNewLine = true;
                _wasSpaceOrTab = false;
                _indentCount = 0;
            }
            else if (next.Type == PythonLexer.WS)
            {
                _wasSpaceOrTab = true;
                if (_wasNewLine)
                {
                    _indentCount += next.Text.Length;
                }
            }
            else
            {
                _wasSpaceOrTab = false;
                _wasNewLine = false;
            }

            _lastToken = next;
            return next;
        }

        protected IToken HandleEndOfFile(IToken eofToken)
        {
            // Add DEDENT tokens for any remaining indentation levels
            while (_indentStack.Count > 1)
            {
                _indentStack.Pop();
                CreateAndAddPendingToken(PythonLexer.DEDENT, eofToken.Line, eofToken.Column, "");
            }

            // Create the EOF token
            CreateAndAddPendingToken(IntStreamConstants.EOF, eofToken.Line, eofToken.Column, "<EOF>");
            return NextToken();
        }

        private void CreateAndAddPendingToken(int ttype, int line, int column, string text)
        {
            CommonToken ctkn = new CommonToken(ttype);
            ctkn.Line = line;
            ctkn.Column = column;
            ctkn.Text = text;
            _pendingTokens.AddLast(ctkn);
        }

        private void ReportLexerError(string errMsg)
        {
            this.ErrorListenerDispatch.SyntaxError(this.ErrorOutput, this, this._lastToken?.Type ?? 0, this._lastToken?.Line ?? 0, this._lastToken?.Column ?? 0, " LEXER" + ERR_TXT + errMsg, null);
        }

        private void ReportError(string errMsg)
        {
            ReportLexerError(errMsg);
            // the ERRORTOKEN will raise an error in the parser
            CreateAndAddPendingToken(PythonLexer.ERRORTOKEN, _lastToken?.Line ?? 0, _lastToken?.Column ?? 0, ERR_TXT + errMsg);
        }

        private static bool IsAtEndOfInput(ICharStream input)
        {
            return input.LA(1) == IntStreamConstants.EOF;
        }

        private static bool IsNotAtEndOfInput(ICharStream input)
        {
            return !IsAtEndOfInput(input);
        }
    }
}
