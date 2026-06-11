using TMPro;
using UnityEngine;

namespace Caretaker.Presentation
{
    public class BookCaseUI : MonoBehaviour
    {
        [Header("Book Title Popup")]
        [SerializeField] private GameObject bookTitlePopup;
        [SerializeField] private TMP_Text titleText;

        private void Awake()
        {
            HideBookTitle();
        }

        public void ShowBookTitle(string title)
        {
            if (bookTitlePopup == null || titleText == null)
            {
                Debug.LogError("[BookCaseUI] BookTitlePopup 또는 TitleText가 연결되지 않았습니다.");
                return;
            }

            bookTitlePopup.SetActive(true);
            bookTitlePopup.transform.SetAsLastSibling();
            titleText.text = title;

            Debug.Log($"[BookCaseUI] 책 제목 확인: {title}");
        }

        public void HideBookTitle()
        {
            if (bookTitlePopup != null)
                bookTitlePopup.SetActive(false);
        }
        
    }
}