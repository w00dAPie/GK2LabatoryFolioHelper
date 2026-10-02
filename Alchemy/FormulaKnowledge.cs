namespace GK2LaboratoryFolioHelper.Alchemy;

internal static class FormulaKnowledge
{
    internal static bool IsFormulaKnown(AlchemyFormulaDef formula)
    {
        if (formula == null || MainGame.Instance?.GameSave?.knowledgeSystem == null)
        {
            return false;
        }

        return MainGame.Instance.GameSave.knowledgeSystem.IsAlchemyFormulaKnown(formula);
    }

    internal static bool IsIngredientKnown(ItemDef itemDef)
    {
        if (itemDef == null)
        {
            return false;
        }

        SurveyDef surveyDef = GameBalance.GetSurveyDefForItemOrNull(itemDef.id);
        if (surveyDef == null)
        {
            return false;
        }

        if (surveyDef.surveyedAtStart)
        {
            return true;
        }

        return MainGame.Instance.GameSave.knowledgeSystem.IsSurveyCompleted(surveyDef);
    }
}
