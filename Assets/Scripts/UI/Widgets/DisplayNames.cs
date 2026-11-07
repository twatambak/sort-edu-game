using OndeVai.Gameplay;

namespace OndeVai.UI
{
    /// <summary>Portuguese names for adult facing screens.</summary>
    public static class DisplayNames
    {
        /// <summary>Name of a group, taken from its item type asset when one exists.</summary>
        public static string Group(ItemGroup group, GameDefinition definition)
        {
            if (definition != null)
            {
                OndeVai.Content.ItemTypeDefinition type = definition.GetItemType(group);
                if (type != null)
                    return type.DisplayName;
            }

            return Group(group);
        }

        public static string Group(ItemGroup group)
        {
            return group switch
            {
                ItemGroup.Toy => "Brinquedos",
                ItemGroup.Food => "Comidas",
                ItemGroup.Tool => "Ferramentas",
                ItemGroup.Animal => "Animais",
                ItemGroup.Letter => "Letras",
                ItemGroup.Number => "Números",
                ItemGroup.HealthyFood => "Comidas saudáveis",
                ItemGroup.UnhealthyFood => "Comidas não saudáveis",
                ItemGroup.GoodBehavior => "Bom comportamento",
                ItemGroup.BadBehavior => "Mau comportamento",
                ItemGroup.Vowel => "Vogais",
                ItemGroup.Consonant => "Consoantes",
                ItemGroup.FarmAnimal => "Animais da fazenda",
                ItemGroup.JungleAnimal => "Animais da selva",
                _ => group.ToString()
            };
        }

        public static string Mode(int mode)
        {
            return (GameMode)mode switch
            {
                GameMode.DragAndDrop => "Arrastar",
                GameMode.Question => "Perguntas",
                _ => "?"
            };
        }

        public static string FinishReason(GameSessionRecord session)
        {
            return session.FinishReason switch
            {
                GameSessionRecord.ReasonItemsCollected => "Meta atingida",
                GameSessionRecord.ReasonTimeExpired => "Tempo esgotado",
                GameSessionRecord.ReasonErrorLimit => "Limite de erros",
                GameSessionRecord.ReasonAbandoned => "Interrompida",
                _ => session.Won ? "Vitória" : "Derrota"
            };
        }

        /// <summary>"85%" or "-" when there is no data.</summary>
        public static string Percent(float ratio)
        {
            return ratio < 0f ? "-" : $"{UnityEngine.Mathf.RoundToInt(ratio * 100f)}%";
        }

        /// <summary>"1h 05min", "12min 30s" or "45s".</summary>
        public static string Duration(double seconds)
        {
            int total = (int)System.Math.Round(seconds);
            int hours = total / 3600;
            int minutes = (total % 3600) / 60;
            int remaining = total % 60;
            if (hours > 0)
                return $"{hours}h {minutes:00}min";

            return minutes > 0 ? $"{minutes}min {remaining:00}s" : $"{remaining}s";
        }
    }
}
