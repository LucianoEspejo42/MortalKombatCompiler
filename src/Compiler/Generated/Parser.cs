
using System;
using System.Collections.Generic;
using System.Linq;
using MortalKombatCompiler.Common.Models;


namespace Generated
{
	public class Parser
	{
		public const int _EOF = 0;
		public const int _UP = 1;
		public const int _DOWN = 2;
		public const int _LEFT = 3;
		public const int _RIGHT = 4;
		public const int _FORWARD = 5;
		public const int _BACK = 6;
		public const int _LP = 7;
		public const int _HP = 8;
		public const int _LK = 9;
		public const int _HK = 10;
		public const int _BL = 11;
		public const int _RUN = 12;
		public const int _TIMING = 13;
		public const int _SEQ_START = 14;
		public const int _SEQ_END = 15;
		public const int maxT = 16;

		const bool _T = true;
		const bool _x = false;
		const int minErrDist = 2;

		public Scanner scanner;
		public Errors errors;

		public Token t;    // last recognized token
		public Token la;   // lookahead token
		int errDist = minErrDist;

		// Variables globales del parser
		public List<TimedInput> inputSequence = new List<TimedInput>();
		public CompilationResult result = new CompilationResult();
		public const int TIMEOUT_MS = 2000;
		public const int DEBOUNCE_MS = 50;



		// Resultado de la compilación
		public class CompilationResult
		{
			public bool Success { get; set; }
			public string MoveType { get; set; } // "FATALITY" o "BRUTALITY"
			public string MoveName { get; set; }
			public List<string> Errors { get; set; } = new List<string>();
			public List<TimedInput> ValidatedSequence { get; set; }
			public string GeneratedCode { get; set; }
		}

		// Definiciones de movimientos de Cyrax
		public static readonly Dictionary<string, List<string>> CyraxMoves = new Dictionary<string, List<string>>
{
	{ "FATALITY_SELF_DESTRUCT", new List<string> { "DOWN", "DOWN", "UP", "DOWN", "HP" } },
	{ "FATALITY_HELICOPTER", new List<string> { "DOWN", "DOWN", "FORWARD", "UP", "RUN" } },
	{ "BRUTALITY_CYRAX", new List<string> { "HP", "LK", "HK", "HK", "LP", "LP", "HP", "LP", "LK", "HK", "LK" } }
};

		// Métodos auxiliares
		private void ValidateTimings()
		{
			for (int i = 0; i < inputSequence.Count; i++)
			{
				var input = inputSequence[i];

				// Validar timeout (excepto el primero)
				if (i > 0 && input.TimingMs > TIMEOUT_MS)
				{
					result.Errors.Add($"Error: Timeout excedido en input {i + 1}. " +
									$"Tiempo: {input.TimingMs}ms, Máximo: {TIMEOUT_MS}ms");
					result.Success = false;
				}

				// Validar debounce (excepto el primero)
				if (i > 0 && input.TimingMs < DEBOUNCE_MS)
				{
					result.Errors.Add($"Error: Inputs demasiado rápidos en posición {i + 1}. " +
									$"Tiempo: {input.TimingMs}ms, Mínimo: {DEBOUNCE_MS}ms");
					result.Success = false;
				}
			}
		}

		private void IdentifyMove()
		{
			var commands = inputSequence.Select(i => i.Command).ToList();

			foreach (var move in CyraxMoves)
			{
				if (commands.SequenceEqual(move.Value))
				{
					result.Success = true;
					result.ValidatedSequence = inputSequence;

					// Identificar el tipo de movimiento
					if (move.Key.StartsWith("FATALITY"))
					{
						result.MoveType = "FATALITY";
						result.MoveName = move.Key.Replace("FATALITY_", "").Replace("_", " ");
					}
					else if (move.Key.StartsWith("BRUTALITY"))
					{
						result.MoveType = "BRUTALITY";
						result.MoveName = move.Key.Replace("BRUTALITY_", "").Replace("_", " ");
					}

					return;
				}
			}

			// Si llegamos aquí, la secuencia no coincide con ningún movimiento
			result.Errors.Add("Error: Secuencia no coincide con ninguna fatality o brutality conocida.");
			result.Success = false;
		}

		private void GenerateIntermediateCode()
		{
			if (!result.Success) return;

			var code = new System.Text.StringBuilder();

			code.AppendLine("// CÓDIGO INTERMEDIO GENERADO");
			code.AppendLine($"// Movimiento: {result.MoveType} - {result.MoveName}");
			code.AppendLine($"// Total de inputs: {inputSequence.Count}");
			code.AppendLine();

			code.AppendLine("EXECUTE {");
			code.AppendLine($"    MOVE_TYPE: {result.MoveType}");
			code.AppendLine($"    MOVE_NAME: {result.MoveName}");
			code.AppendLine("    SEQUENCE: [");

			foreach (var input in inputSequence)
			{
				code.AppendLine($"        {{ COMMAND: \"{input.Command}\", TIMING: {input.TimingMs} }},");
			}

			code.AppendLine("    ]");
			code.AppendLine("    ANIMATION: START");
			code.AppendLine($"    DURATION: {inputSequence.Sum(i => i.TimingMs)}ms");
			code.AppendLine("}");

			result.GeneratedCode = code.ToString();
		}

		// ===========================
		// DEFINICIÓN DE CARACTERES
		// ===========================



		public Parser(Scanner scanner)
		{
			this.scanner = scanner;
			errors = new Errors();
		}

		void SynErr(int n)
		{
			if (errDist >= minErrDist) errors.SynErr(la.line, la.col, n);
			errDist = 0;
		}

		public void SemErr(string msg)
		{
			if (errDist >= minErrDist) errors.SemErr(t.line, t.col, msg);
			errDist = 0;
		}

		void Get()
		{
			for (; ; )
			{
				t = la;
				la = scanner.Scan();
				if (la.kind <= maxT) { ++errDist; break; }

				la = t;
			}
		}

		void Expect(int n)
		{
			if (la.kind == n) Get(); else { SynErr(n); }
		}

		bool StartOf(int s)
		{
			return set[s, la.kind];
		}

		void ExpectWeak(int n, int follow)
		{
			if (la.kind == n) Get();
			else
			{
				SynErr(n);
				while (!StartOf(follow)) Get();
			}
		}


		bool WeakSeparator(int n, int syFol, int repFol)
		{
			int kind = la.kind;
			if (kind == n) { Get(); return true; }
			else if (StartOf(repFol)) { return false; }
			else
			{
				SynErr(n);
				while (!(set[syFol, kind] || set[repFol, kind] || set[0, kind]))
				{
					Get();
					kind = la.kind;
				}
				return StartOf(syFol);
			}
		}


		void MortalKombat()
		{
			result.Success = true;
			Expect(14);
			inputSequence.Clear();
			TimedSequence();
			Expect(15);
			ValidateTimings();
			if (result.Success)
			{
				IdentifyMove();
				GenerateIntermediateCode();
			}

		}

		void TimedSequence()
		{
			TimedInput();
			while (StartOf(1))
			{
				TimedInput();
			}
		}

		void TimedInput()
		{
			string command = "";
			int timing = 0;
			if (StartOf(2))
			{
				Direction(out command);
			}
			else if (StartOf(3))
			{
				Button(out command);
			}
			else SynErr(17);
			if (la.kind == 13)
			{
				Get();
				string timingStr = t.val.Substring(2); // Quitar "T:"
				timing = int.Parse(timingStr);

			}
			inputSequence.Add(new TimedInput
			{
				Command = command,
				TimingMs = timing
			});

		}

		void Direction(out string dir)
		{
			dir = "";
			switch (la.kind)
			{
				case 1:
					{
						Get();
						dir = "UP";
						break;
					}
				case 2:
					{
						Get();
						dir = "DOWN";
						break;
					}
				case 3:
					{
						Get();
						dir = "LEFT";
						break;
					}
				case 4:
					{
						Get();
						dir = "RIGHT";
						break;
					}
				case 5:
					{
						Get();
						dir = "FORWARD";
						break;
					}
				case 6:
					{
						Get();
						dir = "BACK";
						break;
					}
				default: SynErr(18); break;
			}
		}

		void Button(out string btn)
		{
			btn = "";
			switch (la.kind)
			{
				case 7:
					{
						Get();
						btn = "LP";
						break;
					}
				case 8:
					{
						Get();
						btn = "HP";
						break;
					}
				case 9:
					{
						Get();
						btn = "LK";
						break;
					}
				case 10:
					{
						Get();
						btn = "HK";
						break;
					}
				case 11:
					{
						Get();
						btn = "BL";
						break;
					}
				case 12:
					{
						Get();
						btn = "RUN";
						break;
					}
				default: SynErr(19); break;
			}
		}



		public void Parse()
		{
			la = new Token();
			la.val = "";
			Get();
			MortalKombat();
			Expect(0);

		}

		static readonly bool[,] set = {
		{_T,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x},
		{_x,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_x,_x,_x, _x,_x},
		{_x,_T,_T,_T, _T,_T,_T,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x},
		{_x,_x,_x,_x, _x,_x,_x,_T, _T,_T,_T,_T, _T,_x,_x,_x, _x,_x}

	};
	} // end Parser


	public class Errors
	{
		public int count = 0;                                    // number of errors detected
		public System.IO.TextWriter errorStream = Console.Out;   // error messages go to this stream
		public string errMsgFormat = "-- line {0} col {1}: {2}"; // 0=line, 1=column, 2=text

		public virtual void SynErr(int line, int col, int n)
		{
			string s;
			switch (n)
			{
				case 0: s = "EOF expected"; break;
				case 1: s = "UP expected"; break;
				case 2: s = "DOWN expected"; break;
				case 3: s = "LEFT expected"; break;
				case 4: s = "RIGHT expected"; break;
				case 5: s = "FORWARD expected"; break;
				case 6: s = "BACK expected"; break;
				case 7: s = "LP expected"; break;
				case 8: s = "HP expected"; break;
				case 9: s = "LK expected"; break;
				case 10: s = "HK expected"; break;
				case 11: s = "BL expected"; break;
				case 12: s = "RUN expected"; break;
				case 13: s = "TIMING expected"; break;
				case 14: s = "SEQ_START expected"; break;
				case 15: s = "SEQ_END expected"; break;
				case 16: s = "??? expected"; break;
				case 17: s = "invalid TimedInput"; break;
				case 18: s = "invalid Direction"; break;
				case 19: s = "invalid Button"; break;

				default: s = "error " + n; break;
			}
			errorStream.WriteLine(errMsgFormat, line, col, s);
			count++;
		}

		public virtual void SemErr(int line, int col, string s)
		{
			errorStream.WriteLine(errMsgFormat, line, col, s);
			count++;
		}

		public virtual void SemErr(string s)
		{
			errorStream.WriteLine(s);
			count++;
		}

		public virtual void Warning(int line, int col, string s)
		{
			errorStream.WriteLine(errMsgFormat, line, col, s);
		}

		public virtual void Warning(string s)
		{
			errorStream.WriteLine(s);
		}
	} // Errors


	

} // end namespace Generated