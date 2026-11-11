# Molak01 의 자동 초기화 블록에서 변수 목록을 뽑아 MolakVarList.cs 를 만든다(옛 저장 호환용).
import io,re,os
here=os.path.dirname(__file__)
m01=io.open(os.path.join(os.environ.get('MOLAK_CANON', os.path.join(here,'..','canon')),'Molak01.nani'),encoding='utf-8').read()
blk=re.search(r'; \(자동 생성\) 변수 선초기화([\s\S]*?)(?=@initrun)',m01).group(1)
nums=re.findall(r'@set (\w+)=0\n',blk); strs=re.findall(r'@set (\w+)="none"',blk)
# 전역(g_) 숫자 변수: 정본 스크립트 전체에서 쓰이는 이름. 조건식에서 읽기 전에 반드시 있어야 한다(없으면 문자열로 취급돼 비교가 깨진다).
import glob
STR_GLOBALS={'g_lastend','g_lastcause'}
gl=set()
for f in glob.glob(os.path.join(os.environ.get('MOLAK_CANON', os.path.join(here,'..','canon')),'*.nani')):
    gl|=set(re.findall(r'\b(g_[a-z0-9_]*[a-z0-9])\b',io.open(f,encoding='utf-8').read()))
gl=sorted(gl-STR_GLOBALS)
cs='''// (자동 생성: tools2/varlist.py) 회차 변수 목록 — 옛 저장을 불러왔을 때 빠진 변수를 채운다.
using Naninovel;
using UnityEngine;

public static class MolakVarList
{
    public static readonly string[] Nums = { %s };
    public static readonly string[] Strings = { %s };
    /// <summary>스크립트에서 쓰는 전역(g_) 숫자 변수 — InitRunCommand.EnsureGlobals 가 채운다.</summary>
    public static readonly string[] Globals = { %s };
}

/// <summary>@ensurevars — 없는 회차 변수를 기본값으로 채운다(0.4.0 이전 저장 호환). 하루 흐름 맨 앞에서 부른다.</summary>
[Alias("ensurevars")]
public class EnsureVarsCommand : Command
{
#pragma warning disable CS1998
    public override async Awaitable Execute (ExecutionContext ctx)
#pragma warning restore CS1998
    {
        foreach (var n in MolakVarList.Nums) MolakStats.EnsureNum(n);
        foreach (var n in MolakVarList.Strings) MolakStats.EnsureStr(n);
        if (MolakStats.Get("raidday") < 1f) MolakStats.Set("raidday", MolakRules.RaidDay);
        if (MolakStats.Get("thday") < 1f) MolakStats.Set("thday", MolakRules.ThemeHintNever);   // 옛 저장: 바람 귀띔 없이
    }
}
'''%(', '.join('"%s"'%n for n in nums), ', '.join('"%s"'%n for n in strs), ', '.join('"%s"'%n for n in gl))
io.open('/home/claude/molakgeomsin/unity-src/MolakVarList.cs','w',encoding='utf-8').write(cs)
print(len(nums),len(strs),len(gl))
