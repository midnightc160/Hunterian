using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class TypewriterEffect : MonoBehaviour
{
    public float delay = 0.1f;

    public IEnumerator ShowText(string text, TMP_Text tmpText)
    {
        for (int i = 0; i <= text.Length; i++)
        {
            string displayText = text.Substring(0, i);
            tmpText.text = displayText;
            // 更新文本显示
            yield return new WaitForSeconds(0.1f); // 等待一段时间再继续执行
        }
    }
}