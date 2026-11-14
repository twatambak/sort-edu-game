using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using DG.Tweening;
using OndeVai.Data;
using OndeVai.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OndeVai.UI
{
    /// <summary>
    /// Progress report for parents and teachers: headline numbers, accuracy per category and
    /// scenario, the items the child misses most, a chart of the latest matches and their details.
    /// The headline tiles and the scenario and item sections are built at runtime next to the
    /// authored sections.
    /// </summary>
    public sealed class ProgressScreen : UIScreen
    {
        private const int RecentSessionCount = 12;
        private const int ChartSessionCount = 10;
        private const int HardestItemCount = 5;
        private const float TileSpacing = 16f;
        private const float BarSpacing = 14f;
        private const float SectionTitleHeight = 90f;
        private const float RowStagger = 0.04f;
        private const float ChartMinBarHeight = 8f;

        private static readonly Color TileBlue = new(0.33f, 0.63f, 1f, 1f);
        private static readonly Color TileGreen = new(0.3f, 0.78f, 0.45f, 1f);
        private static readonly Color TileOrange = new(1f, 0.62f, 0.26f, 1f);
        private static readonly Color TilePurple = new(0.62f, 0.48f, 0.92f, 1f);
        private static readonly Color TileRed = new(0.95f, 0.4f, 0.4f, 1f);
        private static readonly Color TileTeal = new(0.2f, 0.72f, 0.72f, 1f);
        private static readonly Color LowColor = new(0.95f, 0.45f, 0.4f, 1f);
        private static readonly Color MidColor = new(1f, 0.8f, 0.3f, 1f);
        private static readonly Color HighColor = new(0.4f, 0.8f, 0.45f, 1f);

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
        private readonly List<StatBarRow> _scenarioBars = new();
        private readonly List<StatBarRow> _itemBars = new();
        private readonly List<Image> _chartBars = new();
        private readonly List<TMP_Text> _sessionRows = new();
        private readonly List<ItemTally> _hardestItems = new();
        private readonly StringBuilder _builder = new();
        private static readonly CultureInfo Culture = CreateCulture();

        private bool _runtimeSectionsBuilt;
        private GameObject _tilesRoot;
        private StatTile _gamesTile;
        private StatTile _accuracyTile;
        private StatTile _starsTile;
        private StatTile _playTimeTile;
        private StatTile _responseTile;
        private StatTile _streakTile;
        private RectTransform _scenarioBarsParent;
        private GameObject _scenarioTitle;
        private RectTransform _itemBarsParent;
        private GameObject _itemTitle;

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
            BuildRuntimeSections();

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
                _childLabels.Add(child != null
                    ? $"{child.DisplayName} • {child.UserCode}"
                    : "Sem cadastro");
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

            bool databaseAvailable = data != null && data.IsAvailable;
            ShowTiles(summary, databaseAvailable);
            ShowSummary(summary, databaseAvailable);
            ShowGroupBars(summary);
            ShowScenarioBars(summary);
            ShowHardestItems(summary);
            ShowChart(sessions);
            ShowSessionList(sessions);

            if (_deleteHistoryButton != null)
                _deleteHistoryButton.gameObject.SetActive(child != null && hasData);
        }

        /// <summary>
        /// Creates the headline tiles above the summary text and the scenario and item sections
        /// after the category bars. Runs once, the first time the screen opens.
        /// </summary>
        private void BuildRuntimeSections()
        {
            if (_runtimeSectionsBuilt)
                return;

            _runtimeSectionsBuilt = true;
            if (_summaryText != null)
                BuildTiles();

            if (_groupBarsParent == null || _groupBarTemplate == null)
                return;

            // Inserted in reverse so the final order is: categories, scenarios, hardest items.
            int insertIndex = _groupBarsParent.GetSiblingIndex() + 1;
            _itemBarsParent = CreateBarSection("Itens mais difíceis", insertIndex, out _itemTitle);
            _scenarioBarsParent = CreateBarSection("Acertos por cenário", insertIndex, out _scenarioTitle);
        }

        private void BuildTiles()
        {
            Transform content = _summaryText.transform.parent;
            RectTransform root = RuntimeUI.Rect("StatTiles", content);
            root.SetSiblingIndex(_summaryText.transform.GetSiblingIndex());
            RuntimeUI.Column(root, TileSpacing);
            _tilesRoot = root.gameObject;

            RectTransform firstRow = CreateTileRow(root);
            _gamesTile = new StatTile(firstRow, _summaryText, "Partidas", TileBlue);
            _accuracyTile = new StatTile(firstRow, _summaryText, "Acertos", TileGreen);
            _starsTile = new StatTile(firstRow, _summaryText, "Estrelas", TileOrange);

            RectTransform secondRow = CreateTileRow(root);
            _playTimeTile = new StatTile(secondRow, _summaryText, "Tempo de jogo", TilePurple);
            _responseTile = new StatTile(secondRow, _summaryText, "Resposta média", TileRed);
            _streakTile = new StatTile(secondRow, _summaryText, "Melhor sequência", TileTeal);
        }

        private static RectTransform CreateTileRow(Transform parent)
        {
            RectTransform row = RuntimeUI.Rect("Row", parent);
            RuntimeUI.Row(row, TileSpacing, true);
            return row;
        }

        /// <summary>Adds a titled list of bars that looks like the authored category section.</summary>
        private RectTransform CreateBarSection(string title, int siblingIndex, out GameObject titleObject)
        {
            Transform content = _groupBarsParent.parent;
            RectTransform bars = RuntimeUI.Rect(title + " Bars", content);
            bars.SetSiblingIndex(siblingIndex);
            RuntimeUI.Column(bars, BarSpacing);

            titleObject = CreateSectionTitle(title, content);
            titleObject.transform.SetSiblingIndex(siblingIndex);
            return bars;
        }

        /// <summary>Copies the title above the category bars so new sections share its style.</summary>
        private GameObject CreateSectionTitle(string title, Transform content)
        {
            int authoredTitleIndex = _groupBarsParent.GetSiblingIndex() - 1;
            TMP_Text authoredTitle = authoredTitleIndex >= 0
                ? content.GetChild(authoredTitleIndex).GetComponent<TMP_Text>()
                : null;

            if (authoredTitle != null)
            {
                TMP_Text copy = Instantiate(authoredTitle, content);
                copy.name = title;
                copy.text = title;
                return copy.gameObject;
            }

            TextMeshProUGUI created = RuntimeUI.Text(title, content, _summaryText, title, 52f, TileOrange, TextAlignmentOptions.BottomLeft);
            RuntimeUI.Layout(created.gameObject, -1f, SectionTitleHeight);
            return created.gameObject;
        }

        private void ShowTiles(ChildProgressSummary summary, bool databaseAvailable)
        {
            if (_tilesRoot == null)
                return;

            _tilesRoot.SetActive(databaseAvailable);
            if (!databaseAvailable)
                return;

            _gamesTile.Show(summary.GamesPlayed.ToString(), 0f);
            _accuracyTile.Show(DisplayNames.Percent(summary.Accuracy), RowStagger);
            _starsTile.Show(summary.TotalStars.ToString(), RowStagger * 2f);
            _playTimeTile.Show(DisplayNames.Duration(summary.TotalPlaySeconds), RowStagger * 3f);
            _responseTile.Show(
                summary.AverageResponseSeconds > 0f ? summary.AverageResponseSeconds.ToString("0.0", Culture) + " s" : "-",
                RowStagger * 4f);
            _streakTile.Show(summary.BestStreak > 0 ? summary.BestStreak.ToString() : "-", RowStagger * 5f);
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
            _builder.Append("Vitórias: <b>").Append(summary.Wins).Append("</b> de <b>").Append(summary.GamesPlayed).Append("</b>");
            _builder.Append("    Melhor pontuação: <b>").Append(summary.BestScore).Append("</b>\n");
            _builder.Append("Dias jogados: <b>").Append(summary.DaysPlayed).Append("</b>");
            if (summary.LastPlayedUtc.HasValue)
                _builder.Append("    Última partida: <b>").Append(summary.LastPlayedUtc.Value.ToLocalTime().ToString("dd/MM/yyyy HH:mm", Culture)).Append("</b>");

            // Without the tiles (no summary parent to host them) the text carries every number, as before.
            if (_tilesRoot == null)
            {
                _builder.Append("\nAcertos: <b>").Append(summary.TotalCorrect).Append("</b>");
                _builder.Append("    Erros: <b>").Append(summary.TotalErrors).Append("</b>");
                _builder.Append("    Precisão: <b>").Append(DisplayNames.Percent(summary.Accuracy)).Append("</b>");
                _builder.Append("\nTempo de jogo: <b>").Append(DisplayNames.Duration(summary.TotalPlaySeconds)).Append("</b>");
            }

            _summaryText.text = _builder.ToString();
        }

        private void ShowGroupBars(ChildProgressSummary summary)
        {
            int index = 0;
            foreach (KeyValuePair<ItemGroup, AccuracyTally> entry in summary.AccuracyByGroup)
            {
                StatBarRow row = GetOrCreate(_groupBars, _groupBarTemplate, _groupBarsParent, index);
                if (row == null)
                    return;

                AccuracyTally tally = entry.Value;
                row.Show(DisplayNames.Group(entry.Key, Definition), tally.Ratio, $"{tally.Correct}/{tally.Total}", null, index * RowStagger);
                index++;
            }

            HideFrom(_groupBars, index);
        }

        private void ShowScenarioBars(ChildProgressSummary summary)
        {
            if (_scenarioBarsParent == null)
                return;

            int index = 0;
            foreach (KeyValuePair<string, AccuracyTally> entry in summary.AccuracyByScenario)
            {
                StatBarRow row = GetOrCreate(_scenarioBars, _groupBarTemplate, _scenarioBarsParent, index);
                if (row == null)
                    return;

                AccuracyTally tally = entry.Value;
                row.Show(ScenarioName(entry.Key), tally.Ratio, $"{tally.Correct}/{tally.Total}", null, index * RowStagger);
                index++;
            }

            HideFrom(_scenarioBars, index);
            _scenarioTitle.SetActive(index > 0);
        }

        /// <summary>The items the child gets wrong most often, shown with their picture.</summary>
        private void ShowHardestItems(ChildProgressSummary summary)
        {
            if (_itemBarsParent == null)
                return;

            GameDefinition definition = Definition;
            summary.GetHardestItems(_hardestItems, HardestItemCount);
            for (int i = 0; i < _hardestItems.Count; i++)
            {
                StatBarRow row = GetOrCreate(_itemBars, _groupBarTemplate, _itemBarsParent, i);
                if (row == null)
                    return;

                ItemTally item = _hardestItems[i];
                Sprite icon = definition != null ? definition.FindVariantSprite(item.Group, item.ItemName) : null;
                row.Show(
                    DisplayNames.Group(item.Group, definition),
                    item.Accuracy.Ratio,
                    $"{item.Accuracy.Correct}/{item.Accuracy.Total}",
                    icon,
                    i * RowStagger);
            }

            HideFrom(_itemBars, _hardestItems.Count);
            _itemTitle.SetActive(_hardestItems.Count > 0);
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

                float ratio = AccuracyOf(session);
                RectTransform barRect = bar.rectTransform;
                float width = barRect.sizeDelta.x;
                float height = Mathf.Max(ChartMinBarHeight, ratio * _chartMaxBarHeight);
                bar.color = AccuracyColor(ratio);

                barRect.DOKill();
                barRect.sizeDelta = new Vector2(width, ChartMinBarHeight);
                barRect.DOSizeDelta(new Vector2(width, height), 0.4f)
                    .SetDelay(i * RowStagger)
                    .SetEase(Ease.OutBack)
                    .SetUpdate(true)
                    .SetLink(bar.gameObject);
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
            float ratio = AccuracyOf(session);
            int stars = ProgressCalculator.StarsOf(session);

            _builder.Append("<b>").Append(finished.ToString("dd/MM HH:mm", Culture)).Append("</b>  ");
            _builder.Append(SessionScenarioName(session));

            _builder.Append("\n<size=80%>");
            _builder.Append(DisplayNames.Mode(session.GameMode)).Append("  •  ");
            _builder.Append(DisplayNames.Duration(ProgressCalculator.PlaySecondsOf(session))).Append("  •  ");
            _builder.Append(DisplayNames.FinishReason(session));

            _builder.Append("\n<color=#").Append(ColorUtility.ToHtmlStringRGB(AccuracyColor(ratio))).Append("><b>");
            _builder.Append(DisplayNames.Percent(session.Score + session.Errors > 0 ? ratio : -1f)).Append("</b></color>  •  ");
            _builder.Append("Acertos <b>").Append(session.Score).Append("</b>  •  Erros <b>").Append(session.Errors).Append("</b>  •  ");
            _builder.Append(stars).Append(stars == 1 ? " estrela" : " estrelas");
            if (session.BestStreak > 1)
                _builder.Append("  •  Sequência <b>").Append(session.BestStreak).Append("</b>");

            _builder.Append("</size>");
            return _builder.ToString();
        }

        private string SessionScenarioName(GameSessionRecord session)
        {
            GameDefinition definition = Definition;
            SortingScenarioDefinition scenario = definition != null ? definition.FindScenario(session.ScenarioId) : null;
            if (scenario != null)
                return scenario.DisplayName;

            return DisplayNames.Group((ItemGroup)session.ItemGroupA, definition) + " x " + DisplayNames.Group((ItemGroup)session.ItemGroupB, definition);
        }

        private string ScenarioName(string scenarioId)
        {
            GameDefinition definition = Definition;
            SortingScenarioDefinition scenario = definition != null ? definition.FindScenario(scenarioId) : null;
            return scenario != null ? scenario.DisplayName : "Cenário removido";
        }

        private static float AccuracyOf(GameSessionRecord session)
        {
            int attempts = session.Score + session.Errors;
            return attempts > 0 ? (float)session.Score / attempts : 0f;
        }

        /// <summary>Red for low accuracy, yellow around half and green for high.</summary>
        private static Color AccuracyColor(float ratio)
        {
            return ratio < 0.5f
                ? Color.Lerp(LowColor, MidColor, ratio * 2f)
                : Color.Lerp(MidColor, HighColor, (ratio - 0.5f) * 2f);
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
