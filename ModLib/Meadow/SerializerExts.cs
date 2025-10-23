using System.Collections.Generic;
using ModLib.Options;
using RainMeadow;

namespace ModLib.Meadow;

internal static class SerializerExts
{
    public static void Serialize(this Serializer self, ref Dictionary<string, ConfigValue> data)
    {
        if (self.IsWriting)
        {
            if (data is null)
            {
                self.writer.Write((byte)0);
            }
            else
            {
                self.writer.Write((byte)data.Count);
                foreach (KeyValuePair<string, ConfigValue> kvp in data)
                {
                    self.writer.Write(kvp.Key);
                    self.writer.Write((byte)kvp.Value.Kind);

                    object? value = kvp.Value.GetBoxedValue();
                    switch (value)
                    {
                        case bool:
                            self.writer.Write((bool)value);
                            break;
                        case int:
                            self.writer.Write((int)value);
                            break;
                        case float:
                            self.writer.Write((float)value);
                            break;
                        case string:
                            self.writer.Write((string)value);
                            break;
                        default:
                            self.writer.Write((byte)0);
                            break;
                    }
                }
            }
        }

        if (self.IsReading)
        {
            byte count = self.reader.ReadByte();
            data = new Dictionary<string, ConfigValue>(count);
            for (int i = 0; i < count; i++)
            {
                string key = self.reader.ReadString();

                object? value = (ConfigValue.ValueKind)self.reader.ReadByte() switch
                {
                    ConfigValue.ValueKind.Int => self.reader.ReadInt32(),
                    ConfigValue.ValueKind.Float => self.reader.ReadSingle(),
                    ConfigValue.ValueKind.Bool => self.reader.ReadBoolean(),
                    ConfigValue.ValueKind.String => self.reader.ReadString(),
                    _ => self.reader.ReadByte(),
                };

                data.Add(key, ConfigValue.FromObject(value));
            }
        }
    }
}