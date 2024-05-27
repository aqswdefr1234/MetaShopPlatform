using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public class SameNameChanger
{
    public static string Change_NumInParenthesis(Transform ground, Transform target, string targetName)
    {
        string newName = targetName;
        List<string> nameList = new List<string>();

        foreach (Transform child in ground)
        {
            if (child == target) continue;
            nameList.Add(child.name);
        }

        //같은 이름의 오브젝트가 존재한다면 이름 바꾸기
        int num = 1;
        string pattern = @"\(\d+\)";
        while (nameList.Contains(newName))
        {
            int lastOpenParenIndex = newName.LastIndexOf('(');
            if(lastOpenParenIndex == -1)
            {
                newName += "(1)";
                continue;
            }
            // 이름뒤에 (1) 와 같은 형태가 있으면 잘라낸다.
            if (newName.Length > 2 && MatchPatern(newName.Substring(lastOpenParenIndex), pattern))
            {
                newName = newName.Substring(0, lastOpenParenIndex);
            }
            newName = $"{newName}({num})";
            num++;
        }
        return newName;
    }
    static bool MatchPatern(string input, string pattern)
    {
        Match match = Regex.Match(input, pattern);
        if (match.Success) return true;
        return false;
    }
}
