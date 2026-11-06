// Lecteur / écrivain JSON minimal qui conserve l'ordre des clés et les nombres tels quels.
// Écrit en C# 5 pour être compilable par le csc.exe intégré à Windows (.NET Framework 4.x).
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace LpAccesClaude
{
    public sealed class JObject
    {
        public readonly List<KeyValuePair<string, object>> Items = new List<KeyValuePair<string, object>>();

        public int IndexOf(string key)
        {
            for (int i = 0; i < Items.Count; i++) if (Items[i].Key == key) return i;
            return -1;
        }

        public object Get(string key)
        {
            int i = IndexOf(key);
            return i < 0 ? null : Items[i].Value;
        }

        public bool Has(string key) { return IndexOf(key) >= 0; }

        public void Set(string key, object value)
        {
            int i = IndexOf(key);
            if (i < 0) Items.Add(new KeyValuePair<string, object>(key, value));
            else Items[i] = new KeyValuePair<string, object>(key, value);
        }

        public bool Remove(string key)
        {
            int i = IndexOf(key);
            if (i < 0) return false;
            Items.RemoveAt(i);
            return true;
        }
    }

    public sealed class JArray
    {
        public readonly List<object> Items = new List<object>();
    }

    /// <summary>Nombre JSON conservé sous sa forme texte d'origine.</summary>
    public sealed class JNumber
    {
        public readonly string Raw;
        public JNumber(string raw) { Raw = raw; }
        public override string ToString() { return Raw; }
    }

    /// <summary>Valeur null JSON (distincte d'une clé absente).</summary>
    public sealed class JNull
    {
        public static readonly JNull Value = new JNull();
        private JNull() { }
    }

    public static class Json
    {
        // ------------------------------------------------------------ Lecture
        public static object Parse(string text)
        {
            if (text == null) throw new FormatException("JSON vide");
            int pos = 0;
            if (text.Length > 0 && text[0] == '﻿') pos = 1; // BOM éventuel
            object v = ReadValue(text, ref pos);
            SkipWs(text, ref pos);
            if (pos != text.Length) throw Err(text, pos, "caractères après la fin du JSON");
            return v;
        }

        private static FormatException Err(string s, int pos, string msg)
        {
            int line = 1, col = 1;
            for (int i = 0; i < pos && i < s.Length; i++) { if (s[i] == '\n') { line++; col = 1; } else col++; }
            return new FormatException(string.Format("JSON invalide ligne {0}, colonne {1} : {2}", line, col, msg));
        }

        private static void SkipWs(string s, ref int pos)
        {
            while (pos < s.Length && (s[pos] == ' ' || s[pos] == '\t' || s[pos] == '\r' || s[pos] == '\n')) pos++;
        }

        private static object ReadValue(string s, ref int pos)
        {
            SkipWs(s, ref pos);
            if (pos >= s.Length) throw Err(s, pos, "fin inattendue");
            char c = s[pos];
            if (c == '{') return ReadObject(s, ref pos);
            if (c == '[') return ReadArray(s, ref pos);
            if (c == '"') return ReadString(s, ref pos);
            if (c == 't' && Match(s, pos, "true")) { pos += 4; return true; }
            if (c == 'f' && Match(s, pos, "false")) { pos += 5; return false; }
            if (c == 'n' && Match(s, pos, "null")) { pos += 4; return JNull.Value; }
            if (c == '-' || (c >= '0' && c <= '9')) return ReadNumber(s, ref pos);
            throw Err(s, pos, "valeur inattendue « " + c + " »");
        }

        private static bool Match(string s, int pos, string word)
        {
            return string.CompareOrdinal(s, pos, word, 0, word.Length) == 0;
        }

        private static JObject ReadObject(string s, ref int pos)
        {
            var o = new JObject();
            pos++; // {
            SkipWs(s, ref pos);
            if (pos < s.Length && s[pos] == '}') { pos++; return o; }
            while (true)
            {
                SkipWs(s, ref pos);
                if (pos >= s.Length || s[pos] != '"') throw Err(s, pos, "nom de clé attendu");
                string key = ReadString(s, ref pos);
                SkipWs(s, ref pos);
                if (pos >= s.Length || s[pos] != ':') throw Err(s, pos, "« : » attendu");
                pos++;
                object val = ReadValue(s, ref pos);
                o.Items.Add(new KeyValuePair<string, object>(key, val));
                SkipWs(s, ref pos);
                if (pos >= s.Length) throw Err(s, pos, "fin inattendue dans un objet");
                if (s[pos] == ',') { pos++; continue; }
                if (s[pos] == '}') { pos++; return o; }
                throw Err(s, pos, "« , » ou « } » attendu");
            }
        }

        private static JArray ReadArray(string s, ref int pos)
        {
            var a = new JArray();
            pos++; // [
            SkipWs(s, ref pos);
            if (pos < s.Length && s[pos] == ']') { pos++; return a; }
            while (true)
            {
                a.Items.Add(ReadValue(s, ref pos));
                SkipWs(s, ref pos);
                if (pos >= s.Length) throw Err(s, pos, "fin inattendue dans une liste");
                if (s[pos] == ',') { pos++; continue; }
                if (s[pos] == ']') { pos++; return a; }
                throw Err(s, pos, "« , » ou « ] » attendu");
            }
        }

        private static string ReadString(string s, ref int pos)
        {
            pos++; // "
            var sb = new StringBuilder();
            while (true)
            {
                if (pos >= s.Length) throw Err(s, pos, "texte non terminé");
                char c = s[pos++];
                if (c == '"') return sb.ToString();
                if (c != '\\') { sb.Append(c); continue; }
                if (pos >= s.Length) throw Err(s, pos, "échappement non terminé");
                char e = s[pos++];
                switch (e)
                {
                    case '"': sb.Append('"'); break;
                    case '\\': sb.Append('\\'); break;
                    case '/': sb.Append('/'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case 't': sb.Append('\t'); break;
                    case 'u':
                        if (pos + 4 > s.Length) throw Err(s, pos, "échappement \\u incomplet");
                        int code;
                        if (!int.TryParse(s.Substring(pos, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out code))
                            throw Err(s, pos, "échappement \\u invalide");
                        sb.Append((char)code);
                        pos += 4;
                        break;
                    default: throw Err(s, pos - 1, "échappement inconnu \\" + e);
                }
            }
        }

        private static JNumber ReadNumber(string s, ref int pos)
        {
            int start = pos;
            if (s[pos] == '-') pos++;
            while (pos < s.Length && "0123456789.eE+-".IndexOf(s[pos]) >= 0) pos++;
            string raw = s.Substring(start, pos - start);
            double d;
            if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out d)) throw Err(s, start, "nombre invalide");
            return new JNumber(raw);
        }

        // ------------------------------------------------------------ Écriture
        public static string Serialize(object value)
        {
            var sb = new StringBuilder();
            Write(sb, value, 0);
            return sb.ToString();
        }

        private static void Indent(StringBuilder sb, int level)
        {
            sb.Append('\n');
            sb.Append(' ', level * 2);
        }

        private static void Write(StringBuilder sb, object v, int level)
        {
            if (v == null || v is JNull) { sb.Append("null"); return; }
            if (v is bool) { sb.Append((bool)v ? "true" : "false"); return; }
            if (v is JNumber) { sb.Append(((JNumber)v).Raw); return; }
            if (v is string) { WriteString(sb, (string)v); return; }
            var o = v as JObject;
            if (o != null)
            {
                if (o.Items.Count == 0) { sb.Append("{}"); return; }
                sb.Append('{');
                for (int i = 0; i < o.Items.Count; i++)
                {
                    if (i > 0) sb.Append(',');
                    Indent(sb, level + 1);
                    WriteString(sb, o.Items[i].Key);
                    sb.Append(": ");
                    Write(sb, o.Items[i].Value, level + 1);
                }
                Indent(sb, level);
                sb.Append('}');
                return;
            }
            var a = v as JArray;
            if (a != null)
            {
                if (a.Items.Count == 0) { sb.Append("[]"); return; }
                sb.Append('[');
                for (int i = 0; i < a.Items.Count; i++)
                {
                    if (i > 0) sb.Append(',');
                    Indent(sb, level + 1);
                    Write(sb, a.Items[i], level + 1);
                }
                Indent(sb, level);
                sb.Append(']');
                return;
            }
            throw new InvalidOperationException("Type JSON inconnu : " + v.GetType().Name);
        }

        private static void WriteString(StringBuilder sb, string s)
        {
            sb.Append('"');
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    default:
                        if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4"));
                        else sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
        }
    }
}
