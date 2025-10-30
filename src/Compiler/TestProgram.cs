using System;
using System.IO;
using System.Text;
using Generated;
using Common.Models;

class TestProgram
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== TEST DEL COMPILADOR MORTAL KOMBAT ===\n");

        // Test 1: Fatality Self-Destruct
        string test1 = @"
SEQUENCE_START
DOWN T:0
DOWN T:150
UP T:180
DOWN T:200
HP T:175
SEQUENCE_END
";

        TestCompile("Fatality Self-Destruct", test1);

        // Test 2: Fatality Helicopter
        string test2 = @"
SEQUENCE_START
DOWN T:0
DOWN T:150
FORWARD T:180
UP T:200
RUN T:175
SEQUENCE_END
";

        TestCompile("Fatality Helicopter", test2);

        // Test 3: Secuencia inválida
        string test3 = @"
SEQUENCE_START
UP T:0
UP T:150
HP T:180
SEQUENCE_END
";

        TestCompile("Secuencia Invalida", test3);

        Console.WriteLine("\n=== TESTS COMPLETADOS ===");
    }

    static void TestCompile(string testName, string sourceCode)
    {
        Console.WriteLine($"\n--- Test: {testName} ---");

        try
        {
            var scanner = new Scanner(new MemoryStream(Encoding.UTF8.GetBytes(sourceCode)));
            var parser = new Parser(scanner);

            parser.Parse();

            if (parser.errors.count > 0)
            {
                Console.WriteLine("❌ Errores de sintaxis:");
                return;
            }

            var result = parser.result;

            if (result.Success)
            {
                Console.WriteLine($"✅ Compilación exitosa!");
                Console.WriteLine($"   Tipo: {result.MoveType}");
                Console.WriteLine($"   Nombre: {result.MoveName}");
                Console.WriteLine($"   Inputs: {result.ValidatedSequence.Count}");
                Console.WriteLine($"\nCódigo Intermedio:");
                Console.WriteLine(result.GeneratedCode);
            }
            else
            {
                Console.WriteLine("❌ Errores de compilación:");
                foreach (var error in result.Errors)
                {
                    Console.WriteLine($"   - {error}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Excepción: {ex.Message}");
        }
    }
}