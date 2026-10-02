using System.Collections.Generic;

namespace GK2LaboratoryFolioHelper.Alchemy;

internal sealed class MixCandidate
{
    internal string MixId { get; set; }
    internal int TotalIngredientCount { get; set; }
    internal int OwnedItemCount { get; set; }
    internal int MissingItemCount { get; set; }
    internal int UnobtainableMissingCount { get; set; }
    internal List<MixIngredient> Ingredients { get; set; }
    internal bool IsCraftable => MissingItemCount == 0;
    internal bool ContainsPowder { get; set; }
}

internal sealed class MixIngredient
{
    internal ItemDef ItemDef { get; set; }
    internal int RequiredCount { get; set; }
    internal IngredientAvailability.Result Availability { get; set; }
}
