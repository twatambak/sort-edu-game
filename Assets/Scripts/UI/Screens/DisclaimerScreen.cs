using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OndeVai.UI
{
    /// <summary>
    /// Privacy notice for parents and teachers, shown on first launch and reachable from the options screen.
    /// </summary>
    public sealed class DisclaimerScreen : UIScreen
    {
        public const string Title = "Aviso de Privacidade";

        public const string Body =
            "Este jogo guarda as informações das crianças (nome, idade e desempenho nas partidas) <b>somente neste aparelho</b>.\n\n" +
            "• Os dados ficam armazenados apenas localmente, no próprio dispositivo.\n" +
            "• Nenhuma informação é enviada pela internet, compartilhada com terceiros ou usada para publicidade ou qualquer outra finalidade.\n" +
            "• Os dados servem exclusivamente para que pais, responsáveis e professores acompanhem o progresso da criança dentro do jogo.\n" +
            "• Você pode apagar os dados de cada criança a qualquer momento na área dos responsáveis.\n\n" +
            "Ao continuar, você confirma que é responsável pela criança ou tem autorização para cadastrá-la.";

        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _bodyText;
        [SerializeField] private Button _acceptButton;

        /// <summary>Raised when the adult accepts the notice.</summary>
        public event Action Accepted;

        protected override void Awake()
        {
            base.Awake();
            if (_titleText != null)
                _titleText.text = Title;

            if (_bodyText != null)
                _bodyText.text = Body;

            if (_acceptButton != null)
                _acceptButton.onClick.AddListener(HandleAccept);
            else
                Debug.LogError("DisclaimerScreen has no accept button assigned.", this);
        }

        private void HandleAccept()
        {
            Accepted?.Invoke();
        }
    }
}
