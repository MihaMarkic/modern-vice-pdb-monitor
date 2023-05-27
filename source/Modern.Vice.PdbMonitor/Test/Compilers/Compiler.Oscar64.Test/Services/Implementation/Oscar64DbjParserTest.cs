using Compiler.Oscar64.Models;
using Compiler.Oscar64.Services.Implementation;
using Modern.Vice.PdbMonitor.Utils.Test;
using NUnit.Framework;

namespace Compiler.Oscar64.Test.Services.Implementation;
internal class Oscar64DbjParserTest: BaseTest<Oscar64DbjParser>
{
    [TestFixture]
    public class LoadFileAsync: Oscar64DbjParserTest
    {
        static string LoadSample(string name)
        {
            return File.ReadAllText(Path.Combine("Samples", $"{name}.dbj"));
        }
        [Test]
        public async Task GivenSample_ParsesMemoryCorrectly()
        {
            const string source = """
                {
                    "memory": [
                	    {
                		    "name": "startup",
                		    "start": 2049,
                		    "end": 2134,
                		    "type": "NATIVE_CODE",
                		    "source": "E:/Projects/C64Repo/oscar64/include/crt.c",
                		    "line": 65
                	    },
                	    {
                		    "name": "spentry",
                		    "start": 3354,
                		    "end": 3355,
                		    "type": "DATA",
                		    "source": "E:/Projects/C64Repo/oscar64/include/crt.h",
                		    "line": 4
                	    },
                	    {
                		    "name": "BSSStart",
                		    "start": 3476,
                		    "end": 3476,
                		    "type": "START",
                		    "source": "E:/Projects/C64Repo/oscar64/include/crt.c",
                		    "line": 17
                	    }
                    ]
                }
                """;

            var actual = await Target.LoadContentAsync(source);

            var memory = actual?.Memory;
            Assert.That(memory, Is.EquivalentTo(
                new MemoryBlock[] {
                    new MemoryBlock("startup", 2049, 2134, "NATIVE_CODE", "E:/Projects/C64Repo/oscar64/include/crt.c", 65),
                    new MemoryBlock("spentry", 3354, 3355, "DATA", "E:/Projects/C64Repo/oscar64/include/crt.h", 4),
                    new MemoryBlock("BSSStart", 3476, 3476, "START", "E:/Projects/C64Repo/oscar64/include/crt.c", 17)
                }
                ));
        }
        [Test]
        public async Task GivenSampleFile_DoesNotThrow()
        {
            string content = LoadSample("sprcoltut4");

            var actual = await Target.LoadContentAsync(content);
        }
        [Test]
        public async Task GiveSampleStructType_WithMembers_ParsesMembersCorrectly()
        {
            const string source = """
                {
                    "types": [
                        {"name": "", "typeid": 4, "size": 3, "type": "struct","members": [
                	        {"name": "x", "offset": 0, "typeid": 12},
                	        {"name": "y", "offset": 1, "typeid": 13},
                	        {"name": "z", "offset": 2, "typeid": 14}]}
                        ]
                }
                """;

            var actual = await Target.LoadContentAsync(source);

            var type = actual!.Types.Single();
            Assert.That(type, Is.TypeOf<Oscar64StructType>());
            var structType = (Oscar64StructType)type;
            Assert.That(structType.Members.Length, Is.EqualTo(3));
        }
        [Test]
        public async Task GiveSampleEnumType_WithMembers_ParsesMembersCorrectly()
        {
            const string source = """
                {
                    "types": [
                        {"name": "IOCharMap", "typeid": 2, "size": 1, "type": "enum","members": [
                            {"name": "IOCHM_PETSCII_1", "value": 2},
                            {"name": "IOCHM_ASCII", "value": 1},
                            {"name": "IOCHM_TRANSPARENT", "value": 0}]}
                        ]
                }
                """;

            var actual = await Target.LoadContentAsync(source);

            var type = actual!.Types.Single();
            Assert.That(type, Is.TypeOf<Oscar64EnumType>());
            var structType = (Oscar64EnumType)type;
            Assert.That(structType.Members.Length, Is.EqualTo(3));
        }
        [Test]
        public async Task GiveSampleVoidType_ReturnsVoidType()
        {
            const string source = """
        {
            "types": [
                {"name": "", "typeid": 1, "size": 0}
            ]
        }
        """;

            var actual = await Target.LoadContentAsync(source);

            var type = actual!.Types.Single();
            Assert.That(type, Is.TypeOf<Oscar64VoidType>());
        }
    }
}
