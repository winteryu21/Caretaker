using System.Collections;
using TMPro;
using UnityEngine;

namespace Caretaker.Presentation
{
    public class CofferUI : MonoBehaviour
    {
        [Header("Password UI")]
        [SerializeField] private TMP_Text passwordInputText;

        [Header("Password Setting")]
        [SerializeField] private string correctPassword = "0513";
        [SerializeField] private int maxLength = 4;

        [Header("Close Setting")]
        [SerializeField] private GameObject cofferPanelRoot;
        [SerializeField] private float successCloseDelay = 1f;

        private string currentInput = "";
        private bool isSolved;

        private void OnEnable()
        {
            isSolved = false;
            ClearInput();
        }

        public void PressNumber(string number)
        {
            if (isSolved)
                return;

            if (string.IsNullOrEmpty(number))
                return;

            if (currentInput.Length >= maxLength)
                return;

            currentInput += number;
            RefreshText();

            Debug.Log($"[CofferUI] 숫자 입력: {number}, 현재 입력값: {currentInput}");
        }

        public void DeleteLastNumber()
        {
            if (isSolved)
                return;

            if (string.IsNullOrEmpty(currentInput))
                return;

            currentInput = currentInput.Substring(0, currentInput.Length - 1);
            RefreshText();

            Debug.Log($"[CofferUI] 마지막 숫자 삭제, 현재 입력값: {currentInput}");
        }

        public void ClearInput()
        {
            currentInput = "";
            RefreshText();
        }

        public void Submit()
        {
            if (isSolved)
                return;

            if (currentInput == correctPassword)
            {
                isSolved = true;
                Debug.Log("[CofferUI] 퍼즐 성공: 올바른 비밀번호입니다.");

                StartCoroutine(CloseAfterSuccess());
            }
            else
            {
                Debug.Log("[CofferUI] 퍼즐 실패: 잘못된 비밀번호입니다.");
                ClearInput();
            }
        }

        private IEnumerator CloseAfterSuccess()
        {
            yield return new WaitForSeconds(successCloseDelay);

            if (cofferPanelRoot != null)
                cofferPanelRoot.SetActive(false);
            else
                gameObject.SetActive(false);
        }

        private void RefreshText()
        {
            if (passwordInputText == null)
            {
                Debug.LogError("[CofferUI] PasswordInputText가 연결되지 않았습니다.");
                return;
            }

            passwordInputText.text = currentInput;
        }
    }
}