using System.Collections.Generic;
using System.Globalization;
using System.Text;

/// <summary>
/// 0.5.0 자체 실행기 — 식 평가기. 대본이 쓰는 것만: 숫자·"글자"·변수, + - * / , == != &gt;= &lt;= &gt; &lt; , &amp;&amp; || ! , 괄호, random(a,b).
///   · 식은 한 번 풀어(나무) 두고 다시 쓴다. 없는 변수는 0(글자와 견주면 "").
///   · random(a,b): 둘 다 정수면 a~b 정수(양 끝 포함), 아니면 그 사이 실수 — 나니노벨과 같다.
///   · @set 이름=식 은 Assign 으로. 본문·인자의 {변수} 는 Inject 로.
/// </summary>
public static class MolakExpr
{
    abstract class Node { public abstract MolakVars.Val Eval (); }
    sealed class Num : Node { public float V; public override MolakVars.Val Eval () { return MolakVars.Val.N(V); } }
    sealed class Str : Node { public string V; public override MolakVars.Val Eval () { return MolakVars.Val.S(V); } }
    sealed class Var : Node
    {
        public string Name;
        public override MolakVars.Val Eval () { MolakVars.Val v; if (MolakVars.TryGet(Name, out v)) return v; Missing(Name); return MolakVars.Val.N(0f); }
    }
    sealed class Not : Node { public Node A; public override MolakVars.Val Eval () { return B(!Truthy(A.Eval())); } }
    sealed class Neg : Node { public Node A; public override MolakVars.Val Eval () { return MolakVars.Val.N(-ToNum(A.Eval())); } }
    sealed class Rand : Node
    {
        public Node A, Bn; public bool Ints;
        public override MolakVars.Val Eval ()
        {
            float a = ToNum(A.Eval()), b = ToNum(Bn.Eval());
            if (Ints || (a == (float)System.Math.Floor(a) && b == (float)System.Math.Floor(b))) return MolakVars.Val.N(UnityEngine.Random.Range((int)a, (int)b + 1));
            return MolakVars.Val.N(UnityEngine.Random.Range(a, b));
        }
    }
    sealed class Bin : Node
    {
        public string Op; public Node L, R;
        public override MolakVars.Val Eval ()
        {
            if (Op == "&&") return B(Truthy(L.Eval()) && Truthy(R.Eval()));
            if (Op == "||") return B(Truthy(L.Eval()) || Truthy(R.Eval()));
            var a = L.Eval(); var b = R.Eval();
            if (Op == "==" || Op == "!=")
            {
                bool eq = a.IsStr || b.IsStr ? string.Equals(AsStr(a), AsStr(b), System.StringComparison.Ordinal) : a.Num == b.Num;
                return B(Op == "==" ? eq : !eq);
            }
            if (Op == "+" && (a.IsStr || b.IsStr)) return MolakVars.Val.S(AsStr(a) + AsStr(b));
            double x = ToNum(a), y = ToNum(b);
            switch (Op)
            {
                case "+": return MolakVars.Val.N((float)(x + y));
                case "-": return MolakVars.Val.N((float)(x - y));
                case "*": return MolakVars.Val.N((float)(x * y));
                case "/": return MolakVars.Val.N((float)(x / y));
                case ">": return B(x > y);
                case "<": return B(x < y);
                case ">=": return B(x >= y);
                case "<=": return B(x <= y);
            }
            return MolakVars.Val.N(0f);
        }
    }

    static MolakVars.Val B (bool b) { return MolakVars.Val.N(b ? 1f : 0f); }
    static bool Truthy (MolakVars.Val v) { return v.IsStr ? !string.IsNullOrEmpty(v.Str) && v.Str != "false" : v.Num != 0f; }
    static float ToNum (MolakVars.Val v)
    {
        if (!v.IsStr) return v.Num;
        float f; return float.TryParse(v.Str, NumberStyles.Float, CultureInfo.InvariantCulture, out f) ? f : 0f;
    }
    static string AsStr (MolakVars.Val v) { return v.IsStr ? v.Str ?? "" : MolakVars.Fmt(v.Num); }

    /// <summary>식에서 찾았는데 없던 변수(점검용 — 한 번씩만 적는다).</summary>
    public static readonly HashSet<string> MissingVars = new HashSet<string>();
    static void Missing (string n) { MissingVars.Add(n); }

    // ---- 풀기 ----
    static readonly Dictionary<string, Node> cache = new Dictionary<string, Node>(4096);
    sealed class P
    {
        public string S; public int I;
        public void Ws () { while (I < S.Length && S[I] == ' ') I++; }
        public bool Peek (string op) { Ws(); if (I + op.Length > S.Length) return false; for (int k = 0; k < op.Length; k++) if (S[I + k] != op[k]) return false; return true; }
        public bool Eat (string op) { if (!Peek(op)) return false; I += op.Length; return true; }
    }

    static Node Parse (string s)
    {
        Node n;
        if (cache.TryGetValue(s, out n)) return n;
        var p = new P { S = s };
        n = Or(p); p.Ws();
        if (p.I < s.Length) UnityEngine.Debug.LogWarning("[Molak] 식 끝에 남은 글: '" + s.Substring(p.I) + "' ← " + s);
        cache[s] = n; return n;
    }
    static Node Or (P p) { var l = And(p); while (p.Eat("||")) l = new Bin { Op = "||", L = l, R = And(p) }; return l; }
    static Node And (P p) { var l = Cmp(p); while (p.Eat("&&")) l = new Bin { Op = "&&", L = l, R = Cmp(p) }; return l; }
    static Node Cmp (P p)
    {
        var l = Add(p);
        while (true)
        {
            string op = p.Eat("==") ? "==" : p.Eat("!=") ? "!=" : p.Eat(">=") ? ">=" : p.Eat("<=") ? "<=" : p.Eat(">") ? ">" : p.Eat("<") ? "<" : null;
            if (op == null) return l;
            l = new Bin { Op = op, L = l, R = Add(p) };
        }
    }
    static Node Add (P p)
    {
        var l = Mul(p);
        while (true)
        {
            if (p.Eat("+")) l = new Bin { Op = "+", L = l, R = Mul(p) };
            else if (p.Peek("-")) { p.I++; l = new Bin { Op = "-", L = l, R = Mul(p) }; }
            else return l;
        }
    }
    static Node Mul (P p)
    {
        var l = Un(p);
        while (true)
        {
            if (p.Eat("*")) l = new Bin { Op = "*", L = l, R = Un(p) };
            else if (p.Eat("/")) l = new Bin { Op = "/", L = l, R = Un(p) };
            else return l;
        }
    }
    static Node Un (P p)
    {
        p.Ws();
        if (p.I < p.S.Length && p.S[p.I] == '!' && !(p.I + 1 < p.S.Length && p.S[p.I + 1] == '=')) { p.I++; return new Not { A = Un(p) }; }
        if (p.I < p.S.Length && p.S[p.I] == '-') { p.I++; return new Neg { A = Un(p) }; }
        if (p.I < p.S.Length && p.S[p.I] == '+') { p.I++; return Un(p); }
        return Atom(p);
    }
    static Node Atom (P p)
    {
        p.Ws();
        if (p.I >= p.S.Length) return new Num { V = 0f };
        char c = p.S[p.I];
        if (c == '(') { p.I++; var n = Or(p); p.Eat(")"); return n; }
        if (c == '"')
        {
            var sb = new StringBuilder(); p.I++;
            while (p.I < p.S.Length && p.S[p.I] != '"') { if (p.S[p.I] == '\\' && p.I + 1 < p.S.Length) p.I++; sb.Append(p.S[p.I]); p.I++; }
            p.I++; return new Str { V = sb.ToString() };
        }
        if (char.IsDigit(c) || c == '.')
        {
            int s0 = p.I; while (p.I < p.S.Length && (char.IsDigit(p.S[p.I]) || p.S[p.I] == '.')) p.I++;
            return new Num { V = float.Parse(p.S.Substring(s0, p.I - s0), CultureInfo.InvariantCulture) };
        }
        if (char.IsLetter(c) || c == '_')
        {
            int s0 = p.I; while (p.I < p.S.Length && (char.IsLetterOrDigit(p.S[p.I]) || p.S[p.I] == '_')) p.I++;
            string id = p.S.Substring(s0, p.I - s0);
            p.Ws();
            if (p.I < p.S.Length && p.S[p.I] == '(')
            {
                p.I++; var a = Or(p); p.Eat(","); var b = Or(p); p.Eat(")");
                if (id == "random" || id == "Random") return new Rand { A = a, Bn = b, Ints = a is Num && b is Num && ((Num)a).V == (float)System.Math.Floor(((Num)a).V) && ((Num)b).V == (float)System.Math.Floor(((Num)b).V) };
                UnityEngine.Debug.LogWarning("[Molak] 모르는 함수 " + id); return new Num { V = 0f };
            }
            if (id == "true") return new Num { V = 1f };
            if (id == "false") return new Num { V = 0f };
            return new Var { Name = id };
        }
        UnityEngine.Debug.LogWarning("[Molak] 식을 못 읽는다: " + p.S + " @" + p.I);
        p.I = p.S.Length; return new Num { V = 0f };
    }

    public static MolakVars.Val Eval (string expr) { return Parse(expr).Eval(); }
    public static bool True (string expr) { return string.IsNullOrEmpty(expr) || Truthy(Parse(expr).Eval()); }
    public static float Number (string expr) { return ToNum(Parse(expr).Eval()); }

    /// <summary>@set 의 "이름=식"(여럿이면 ; 로). 글자 식이면 글자 변수, 아니면 숫자.</summary>
    public static void Assign (string stmt)
    {
        if (stmt.IndexOf(';') >= 0 && stmt.IndexOf('"') < 0) { foreach (var part in stmt.Split(';')) if (part.Trim().Length > 0) Assign1(part.Trim()); }
        else Assign1(stmt);
    }
    static void Assign1 (string stmt)
    {
        int eq = stmt.IndexOf('=');
        if (eq <= 0) { UnityEngine.Debug.LogWarning("[Molak] @set 꼴이 아니다: " + stmt); return; }
        string name = stmt.Substring(0, eq).Trim(), rhs = stmt.Substring(eq + 1);
        // += -= 꼴(대본에는 없지만 받아 둔다)
        if (name.EndsWith("+") || name.EndsWith("-")) { char op = name[name.Length - 1]; name = name.Substring(0, name.Length - 1).Trim(); rhs = name + op + "(" + rhs + ")"; }
        var v = Parse(rhs).Eval();
        MolakVars.Set(name, v);
    }

    /// <summary>글·인자 속 {변수} 를 값으로. 없는 변수는 빈 글.</summary>
    public static string Inject (string t)
    {
        if (string.IsNullOrEmpty(t) || t.IndexOf('{') < 0) return t;
        var sb = new StringBuilder(t.Length + 32);
        for (int i = 0; i < t.Length; i++)
        {
            if (t[i] == '{')
            {
                int e = t.IndexOf('}', i + 1);
                if (e > i)
                {
                    string inner = t.Substring(i + 1, e - i - 1).Trim();
                    MolakVars.Val v;
                    if (IsIdent(inner)) { if (MolakVars.TryGet(inner, out v)) sb.Append(AsStr(v)); }
                    else sb.Append(AsStr(Parse(inner).Eval()));
                    i = e; continue;
                }
            }
            sb.Append(t[i]);
        }
        return sb.ToString();
    }
    static bool IsIdent (string s) { if (s.Length == 0) return false; foreach (char c in s) if (!(char.IsLetterOrDigit(c) || c == '_')) return false; return true; }
}
