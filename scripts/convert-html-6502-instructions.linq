<Query Kind="Program">
  <Namespace>System.Collections.Immutable</Namespace>
  <Namespace>System.Globalization</Namespace>
</Query>

// Source of HTML is http://www.6502.org/users/obelisk/6502/reference.html#LDA

void Main()
{
	string directory = Path.GetDirectoryName(Util.CurrentQueryPath)!;
	string htmlFile = Path.Combine(directory, "instructions.html");
	string source = File.ReadAllText(htmlFile);

	var reader = new StringReader(source);
	var builder = new StringBuilder();
	string? line;
	while ((line = reader.ReadLine()) is not null)
	{
	 	if (line.Trim() != "<P>")
		{
			builder.AppendLine(line);
		}
	}
	var xelement = XElement.Parse(source.Replace("<BR>", "").Replace("&nbsp;", "").Replace("<P>", "").Replace("</P>","")
	.Replace("<P align=\"center\">", "")
	.ToString());
	var items = new List<Item>();
	Item? item;
	var elements = xelement.Elements().ToImmutableArray();
	int i = 0;
	while (i < elements.Length)
	{
		var node = elements[i];
		if (node.Name == "H3")
		{
			item = new Item((string)node.Elements("A").Attributes("NAME").Single());
			items.Add(item);
			i += 2;
			var addressingTable = elements[i];
			var rows = addressingTable.Elements("TR").Skip(1).ToImmutableArray();
			foreach (var row in rows)
			{
				var modes = row;
				var tds = modes.Elements("TD")!.ToImmutableArray();
				string mode = (string)tds[0].Element("A")!.Value;
				string opCode = tds[1].Element("CENTER")!.Value;
				string cycles = tds[3].Value;
				item.Modes.Add(
					new Mode(
						mode,
						byte.Parse(opCode[1..], NumberStyles.HexNumber, CultureInfo.InvariantCulture),
						byte.Parse(cycles[..1]),
						cycles
					));
			}
		}
		i++;
	}
	//GenerateSwitch(items);
	var root = new XElement("Instructions",
		items.Select(i => new XElement("Instruction", new XAttribute("Name", i.Name),
			i.Modes.Select(m => 
				new XElement("Mode", 
					new XAttribute("Name", m.Name),
					new XAttribute("OpCode", m.OpCode.ToString("X2")),
					new XAttribute("Cycles", m.Cycles),
					new XAttribute("CyclesInfo", m.CyclesInfo)
				))))
	);
	root.Dump();
}

public record Item(string Name)
{
	public List<Mode> Modes { get; } = new ();
}
public record Mode(string Name, byte OpCode, byte Cycles, string CyclesInfo);