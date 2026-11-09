using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using OndeVai.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using OndeVai.Gameplay;

namespace OndeVai.UI
{
    /// <summary>
    /// Progress report for parents and teachers: overall numbers, accuracy per category and the
    /// most recent matches of the selected child.
    /// </summary>
    public sealed class ProgressScreen : UIScreen
    {
        private const int RecentSessionCount = 12;
        private const int ChartSessionCount = 10;

        [SerializeField] private OptionSelector _childSelector;
        [SerializeField] private AvatarBadge _childBadge;
        [SerializeField] private TMP_Text _summaryText;
        [SerializeField] private GameObject _noDataMessage;

        [Header("Accuracy per category")]
        [SerializeField] private RectTransform _groupBarsParent;
        [SerializeField] private StatBarRow _groupBarTemplate;

        [Header("Recent matches chart")]
        [SerializeField] private RectTransform _chartParent;
        [SerializeField] private Image _chartBarTemplate;
        [SerializeField, Min(10f)] private float _chartMaxBarHeight = 220f;

        [Header("Recent matches list")]
        [SerializeField] private RectTransform _sessionListParent;
        [SerializeField] private TMP_Text _sessionRowTemplate;

        [Header("Actions")]
        [SerializeField] private HoldToConfirmButton _deleteHistoryButton;
        [SerializeField] private Button _backButton;

        private readonly List<ChildProfileRecord> _children = new();
        private readonly List<string> _childLabels = new();
        private readonly List<StatBarRow> _groupBars = new();
        private readonly List<Image> _chartBars = new();
        private readonly List<TMP_Text> _sessionRows = new();
        private readonly StringBuilder _builder = new();
        private static readonly CultureInfo Culture = CreateCulture();

        public event Action Closed;

        private GameDataService Data => GameController.Instance != null ? GameController.Instance.Data : null;

        private GameDefinition Definition => GameController.Instance != null ? GameController.Instance.Definition : null;

        protected override void Awake()
        {
            base.Awake();
            HideTemplate(_groupBarTemplate);
            HideTemplate(_chartBarTemplate);
            HideTemplate(_sessionRowTemplate);

            if (_childSelector != null)
                _childSelector.Changed += _ => ShowSelectedChild();

            if (_backButton != null)
                _backButton.onClick.AddListener(() => Closed?.Invoke());

            if (_deleteHistoryButton != null)
                _deleteHistoryButton.OnConfirmed.AddListener(DeleteSelectedHistory);
        }

        protected override void OnShown()
        {
            GameDataService data = Data;
            _children.Clear();
            _childLabels.Clear();

            if (data != null)
                _children.AddRange(data.GetProfiles());

            _children.Add(null);
            int selected = _children.Count - 1;
            for (int i = 0; i < _children.Count; i++)
            {
                ChildProfileRecord child = _children[i];
                _childLabels.Add(child != null ? child.Name : "Sem cadastro");
                if (child != null && data?.ActiveChild != null && child.Id == data.ActiveChild.Id)
                    selected = i;
            }

            if (_childSelector != null)
                _childSelector.SetOptions(_childLabels, selected);

            ShowSelectedChild();
        }

        private ChildProfileRecord SelectedChild =>
            _childSelector != null && _childSelector.Index < _children.Count ? _children[_childSelector.Index] : null;

        private void ShowSelectedChild()
        {
            GameDataService data = Data;
            ChildProfileRecord child = SelectedChild;
            int childId = child?.Id ?? 0;

            if (_childBadge != null)
                _childBadge.Show(child);

            ChildProgressSummary summary = data != null ? data.BuildSummary(childId) : new ChildProgressSummary();
            List<GameSessionRecord> sessions = data != null ? data.GetRecentSessions(childId, RecentSessionCount) : new List<GameSessionRecord>();

            bool hasData = summary.GamesPlayed > 0;
            if (_noDataMessage != null)
                _noDataMessage.SetActive(!hasData);

            ShowSummary(summary, data != null && data.IsAvailable);
            ShowGroupBars(summary);
            ShowChart(sessions);
            ShowSessionList(sessions);

            if (_deleteHistoryButton != null)
                _deleteHistoryButton.gameObject.SetActive(child != null && hasData);
        }

        private void ShowSummary(ChildProgressSummary summary, bool databaseAvailable)
        {
            if (_summaryText == null)
                return;

            if (!databaseAvailable)
            {
                _summaryText.text = "Os dados deste aparelho não estão disponíveis.";
                return;
            }

            _builder.Clear();
            _builder.Append("Partidas: <b>").Append(summary.GamesPlayed).Append("</b>");
            _builder.Append("    Vitórias: <b>").Append(summary.Wins).Append("</b>\n");
            _builder.Append("Acertos: <b>").Append(summary.TotalCorrect).Append("</b>");
            _builder.Append("    Erros: <b>").Append(summary.TotalErrors).Append("</b>\n");
            _builder.Append("Precisão: <b>").Append(DisplayNames.Percent(summary.Accuracy)).Append("</b>");
            _builder.Append("    Melhor pontuação: <b>").Append(summary.BestScore).Append("</b>\n");
            _builder.Append("Tempo de jogo: <b>").Append(DisplayNames.Duration(summary.TotalPlaySeconds)).Append("</b>");
            if (summary.AverageResponseSeconds > 0f)
                _builder.Append("    Resposta média: <b>").Append(summary.AverageResponseSeconds.ToString("0.0", Culture)).Append(" s</b>");

            if (summary.LastPlayedUtc.HasValue)
                _builder.Append("\nÚltima partida: <b>").Append(summary.LastPlayedUtc.Value.ToLocalTime().ToString("dd/MM/yyyy HH:mm", Culture)).Append("</b>");

            _summaryText.text = _builder.ToString();
        }

        private void ShowGroupBars(ChildProgressSummary summary)
        {
            int index = 0;
            foreach (KeyValuePair<ItemGroup, AccuracyTally> entry in summary.AccuracyByGroup)
            {
                StatBarRow row = GetOrCreate(_groupBars, _groupBarTemplate, _groupBarsParent, index++);
                if (row == null)
                    return;

                AccuracyTally tally = entry.Value;
                row.Show(DisplayNames.Group(entry.Key, Definition), tally.Ratio, $"{tally.Correct}/{tally.Total}");
            }

            HideFrom(_groupBars, index);
        }

        private void ShowChart(List<GameSessionRecord> sessions)
        {
            int count = Mathf.Min(ChartSessionCount, sessions.Count);
            for (int i = 0; i < count; i++)
            {
                // Oldest on the left: sessions are sorted newest first.
                GameSessionRecord session = sessions[count - 1 - i];
                Image bar = GetOrCreate(_chartBars, _chartBarTemplate, _chartParent, i);
                if (bar == null)
                    return;

                int attempts = session.Score + session.Errors;
                float ratio = attempts > 0 ? (float)session.Score / attempts : 0f;
                RectTransform barRect = bar.rectTransform;
                barRect.sizeDelta = new Vector2(barRect.sizeDelta.x, Mathf.Max(8f, ratio * _chartMaxBarHeight));
                bar.color = Color.Lerp(new Color(0.95f, 0.45f, 0.4f), new Color(0.4f, 0.8f, 0.45f), ratio);
            }

            HideFrom(_chartBars, count);
        }

        private void ShowSessionList(List<GameSessionRecord> sessions)
        {
            for (int i = 0; i < sessions.Count; i++)
            {
                TMP_Text row = GetOrCreate(_sessionRows, _sessionRowTemplate, _sessionListParent, i);
                if (row == null)
                    return;

                row.text = FormatSession(sessions[i]);
            }

            HideFrom(_sessionRows, sessions.Count);
        }

        private string FormatSession(GameSessionRecord session)
        {
            _builder.Clear();
            DateTime finished = new DateTime(session.FinishedAtUtcTicks, DateTimeKind.Utc).ToLocalTime();
            _builder.Append(finished.ToString("dd/MM HH:mm", Culture)).Append("  ");
            _builder.Append(DisplayNames.Mode(session.GameMode)).Append("  ");
            if ((GameMode)session.GameMode == GameMode.FourWaySwipe)
            {
                SortingScenarioDefinition scenario = Definition != null
                    ? Definition.GetScenarioOrDefault(session.ScenarioId, 4)
                    : null;
                if (scenario != null)
                {
                    _builder.Append("→ ").Append(DisplayNames.Group(scenario.GroupA, Definition))
                        .Append("  ← ").Append(DisplayNames.Group(scenario.GroupB, Definition))
                        .Append("  ↑ ").Append(DisplayNames.Group(scenario.GroupC, Definition))
                        .Append("  ↓ ").Append(DisplayNames.Group(scenario.GroupD, Definition));
                }
                else
                {
                    _builder.Append(session.ScenarioId);
                }
            }
            else
            {
                _builder.Append(DisplayNames.Group((ItemGroup)session.ItemGroupA, Definition))
                    .Append(" x ").Append(DisplayNames.Group((ItemGroup)session.ItemGroupB, Definition));
            }

            _builder.Append("\n<size=80%>Acertos <b>").Append(session.Score).Append("</b>  Erros <b>").Append(session.Errors).Append("</b>  ");
            _builder.Append(DisplayNames.Duration(session.DurationSeconds)).Append("  ").Append(DisplayNames.FinishReason(session)).Append("</size>");
            return _builder.ToString();
        }

        private void DeleteSelectedHistory()
        {
            ChildProfileRecord child = SelectedChild;
            if (child == null)
                return;

            GameDataService data = Data;
            if (data == null || !data.DeleteHistory(child))
            {
                if (_summaryText != null)
                    _summaryText.text = "Não foi possível apagar o histórico deste perfil.";
                return;
            }

            ShowSelectedChild();
        }

        private static T GetOrCreate<T>(List<T> pool, T template, RectTransform parent, int index) where T : Component
        {
            if (template == null || parent == null)
                return null;

            while (pool.Count <= index)
                pool.Add(Instantiate(template, parent));

            T item = pool[index];
            item.gameObject.SetActive(true);
            return item;
        }

        private static void HideFrom<T>(List<T> pool, int startIndex) where T : Component
        {
            for (int i = startIndex; i < pool.Count; i++)
                pool[i].gameObject.SetActive(false);
        }

        private static CultureInfo CreateCulture()
        {
            try
            {
                return new CultureInfo("pt-BR");
            }
            catch (CultureNotFoundException)
            {
                return CultureInfo.InvariantCulture;
            }
        }

        private static void HideTemplate(Component template)
        {
            if (template != null)
                template.gameObject.SetActive(false);
        }
    }
}
