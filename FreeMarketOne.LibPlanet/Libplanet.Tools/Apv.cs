using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Bencodex;
using Bencodex.Types;
using Cocona;
using Libplanet.Crypto;
using Libplanet.Net;

namespace Libplanet.Tools
{
    public class Apv
    {
        [Command(Description = "Sign a new app protocol version.")]
        public void Sign(
            [Argument(Name = "KEY-ID", Description = "A private key to use for signing.")]
            Guid keyId,
            [Argument(Name = "VERSION", Description = "A version number to sign.")]
            int version,
            [Option(
                'p',
                ValueName = "PASSPHRASE",
                Description = "Take passphrase through this option instead of prompt."
            )]
            string? passphrase = null,
            [Option(
                'E',
                ValueName = "FILE",
                Description = "Bencodex file to use for extra data.  " +
                    "For standard input, use a hyphen (`-').  " +
                    "For an actual file named a hyphen, prepend `./', i.e., `./-'."
            )]
            string? extraFile = null,
            [Option(
                'e',
                ValueName = "KEY=VALUE",
                Description = "Set a value to a key on extra Bencodex dictionary.  " +
                    "Can be applied multiple times (e.g., `-e foo=1 -e bar=baz').  " +
                    "This option implies the extra data to be a Bencodex dictionary, " +
                    "hence cannot be used together with -E/--extra-file option."
            )]
            string[]? extra = null
        )
        {
            PrivateKey key = new Key().UnprotectKey(keyId, passphrase);
            IValue? extraValue = null;
            if (extraFile is string path)
            {
                if (extra is string[] e && e.Length > 0)
                {
                    throw Utils.Error(
                        "-E/--extra-file and -e/--extra cannot be used together at a time."
                    );
                }

                var codec = new Codec();
                if (path == "-")
                {
                    // Stream for stdin does not support .Seek()
                    using MemoryStream buffer = new MemoryStream();
                    using (Stream stream = Console.OpenStandardInput())
                    {
                        stream.CopyTo(buffer);
                    }

                    buffer.Seek(0, SeekOrigin.Begin);
                    extraValue = codec.Decode(buffer);
                }
                else
                {
                    using Stream stream = File.Open(path, FileMode.Open, FileAccess.Read);
                    extraValue = codec.Decode(stream);
                }
            }
            else if (extra is string[] e && e.Length > 0)
            {
                var dict = Bencodex.Types.Dictionary.Empty;
                foreach (string pair in e)
                {
                    int sepPos = pair.IndexOf('=');
                    if (sepPos < 0)
                    {
                        throw Utils.Error(
                            "-e/--extra must be a pair of KEY=VALUE, but no equal (=) separator: " +
                            $"`{pair}'."
                        );
                    }

                    string key_ = pair.Substring(0, sepPos);
                    string value = pair.Substring(sepPos + 1);
                    dict = dict.SetItem(key_, value);
                }

                extraValue = dict;
            }

            AppProtocolVersion v = AppProtocolVersion.Sign(key, version, extraValue);
            Console.WriteLine(v.Token);
        }

        [Command(Description = "Parse and analyze a given app protocol version token.")]
        public void Analyze(
            [Argument(
                Name = "APV-TOKEN",
                Description = "An app protocol version token to analyze.  " +
                    "Read from the standard input if omitted."
            )]
            string? token = null
        )
        {
            if (token is null)
            {
                token = Console.ReadLine();
            }

            AppProtocolVersion v;
            try
            {
                v = AppProtocolVersion.FromToken(token.Trim());
            }
            catch (FormatException e)
            {
                throw Utils.Error("Not a valid app protocol version token.  " + e);
            }

            var data = new List<(string, string)>
            {
                ("version", v.Version.ToString(CultureInfo.InvariantCulture)),
                ("signature", ByteUtil.Hex(v.Signature)),
                ("signer", v.Signer.ToString()),
            };

            if (v.Extra is IValue extra)
            {
                void TreeIntoTable(IValue tree, List<(string, string)> table, string key)
                {
                    switch (tree)
                    {
                        case Null _:
                            table.Add((key, "null"));
                            return;

                        case Bencodex.Types.Boolean b:
                            table.Add((key, b ? "true" : "false"));
                            return;

                        case Binary bin:
                            table.Add((key, ByteUtil.Hex(bin.Value)));
                            return;

                        case Text t:
                            table.Add((key, t.Value));
                            return;

                        case Bencodex.Types.Integer i:
                            table.Add((key, i.Value.ToString(CultureInfo.InvariantCulture)));
                            return;

                        case Bencodex.Types.List l:
                            int idx = 0;
                            foreach (IValue el in l)
                            {
                                TreeIntoTable(el, table, $"{key}[{idx}]");
                                idx++;
                            }

                            return;

                        case Bencodex.Types.Dictionary d:
                            foreach (KeyValuePair<IKey, IValue> kv in d)
                            {
                                string k = kv.Key switch
                                {
                                    Binary bk => ByteUtil.Hex(bk),
                                    Text txt => txt.Value,
                                    _ => kv.Key.ToString() ?? string.Empty,
                                };
                                TreeIntoTable(kv.Value, table, $"{key}.{k}");
                            }

                            return;

                        default:
                            table.Add((key, tree.ToString() ?? string.Empty));
                            return;
                    }
                }

                TreeIntoTable(v.Extra, data, "extra");
            }

            Utils.PrintTable(("Field", "Value"), data);
        }
    }
}
