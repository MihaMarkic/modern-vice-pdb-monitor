<Query Kind="Program">
  <Namespace>System.Collections.Immutable</Namespace>
  <Namespace>System.Globalization</Namespace>
</Query>

// Source of HTML is http://www.6502.org/users/obelisk/6502/reference.html#LDA

void Main()
{
	string directory = Path.GetDirectoryName(Util.CurrentQueryPath)!;
	string xmlFile = Path.Combine(directory, "instructions.xml");
	var root = XDocument.Load(xmlFile);
	var builder = ImmutableArray.CreateBuilder<Item>();
	foreach (var i in root.Root!.Elements("Instruction"))
	{
		var item = new Item((string)(i.Attribute("Name")!));
		builder.Add(item);
		foreach (var m in i.Elements("Mode"))
		{
			item.Modes.Add(new Mode(
				(string)m.Attribute("Name")!,
				byte.Parse((string)m.Attribute("OpCode")!, NumberStyles.HexNumber, CultureInfo.InvariantCulture),
				byte.Parse((string)m.Attribute("Cycles")!, NumberStyles.HexNumber, CultureInfo.InvariantCulture),
				(string)m.Attribute("CyclesInfo")!
			));
		}
	}
	var items = builder.ToImmutable();
	//items.Dump();
	GenerateSwitch(items);
}

string GenerateMode(Mode mode, byte opCode) 
{
	string result = mode.Name switch
	{
		"Immediate" => $"ImmediateInstructionMode(0x{opCode:X2}, data[1]",
		"Zero Page" => $"ZeroPageInstructionMode(0x{opCode:X2}, data[1]",
		"Zero Page,X" => $"ZeroPageXInstructionMode(0x{opCode:X2}, data[1]",
		"Zero Page,Y" => $"ZeroPageYInstructionMode(0x{opCode:X2}, data[1]",
		"Absolute" => $"AbsoluteInstructionMode(0x{opCode:X2}, data[1], data[2]",
		"Absolute,X" => $"AbsoluteXInstructionMode(0x{opCode:X2}, data[1], data[2]",
		"Absolute,Y" => $"AbsoluteYInstructionMode(0x{opCode:X2}, data[1], data[2]",
		"(Indirect,X)" => $"IndirectXInstructionMode(0x{opCode:X2}, data[1]",
		"(Indirect),Y" => $"IndirectYInstructionMode(0x{opCode:X2}, data[1]",
		"Accumulator" => $"AccumulatorInstructionMode(0x{opCode:X2}",
		"Implied" => $"ImpliedInstructionMode(0x{opCode:X2}",
		"Relative" => $"RelativeInstructionMode(0x{opCode:X2}, (sbyte)data[1]",
		"Indirect" => $"IndirectInstructionMode(0x{opCode:X2}, data[1], data[2]",
		_ => throw new Exception($"Invalid mode {mode.Name}")
	};
	return $"new {result}, {mode.Cycles})";
}

void GenerateSwitch(IList<Item> items)
{
	string prefix = "\t\t\t";
	foreach (var item in items)
	{
		$"{prefix}// {item.Name}".Dump();
		foreach (var mode in item.Modes)
		{
			string modeText = GenerateMode(mode, mode.OpCode);
			$"{prefix}0x{mode.OpCode:X2} => new Instruction(InstructionName.{item.Name}, {modeText}),".Dump();
		}
	}
}

public record Item(string Name)
{
	public List<Mode> Modes { get; } = new ();
}
public record Mode(string Name, byte OpCode, byte Cycles, string CyclesInfo);