using System;
using System.Linq;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Harmony;
using System.Reflection;

namespace Angels
{
    [StaticConstructorOnStartup]
    public static class AngelsLoader {
        static AngelsLoader()
        {
            var harmony = HarmonyInstance.Create("net.mseal.rimworld.mod.angels");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }

    [HarmonyPatch(typeof(PawnTechHediffsGenerator), "GenerateTechHediffsFor", new Type[] { typeof(Pawn) })]
    internal static class HediffsMultipleModsPatch {
    	private static List<Thing> emptyIngredientsList = new List<Thing>();

        private static bool Prefix(Pawn pawn)
        {
        	if (pawn.kindDef.techHediffsTags == null)
			{
				return false;
			}
			if (Rand.Value > pawn.kindDef.techHediffsChance)
			{
				return false;
			}
			float partsMoney = pawn.kindDef.techHediffsMoney.RandomInRange;
            // Default to normal 1 modification per unit
            int maxParts = new DefModExtension_AngelPawn().maxHediffs;
            if (pawn.kindDef.HasModExtension<DefModExtension_AngelPawn>()) {
                maxParts = pawn.kindDef.GetModExtension<DefModExtension_AngelPawn>().maxHediffs;
            }
            // Safe guard for mod load issues
            if (maxParts == null)
            {
                Log.ErrorOnce("maxHediffs left undefined for pawn of type " + pawn.def.defName, 999991);
                maxParts = 1;
            }
            int checkCount = 0;
            int partCount = 0;
			while (partsMoney > 0.0f && partCount < maxParts && checkCount < 50) {
				checkCount++;
				IEnumerable<ThingDef> source = from x in DefDatabase<ThingDef>.AllDefs
				where x.isTechHediff && x.BaseMarketValue <= partsMoney && x.techHediffsTags != null && pawn.kindDef.techHediffsTags.Any((string tag) => x.techHediffsTags.Contains(tag))
				select x;
				if (source.Any<ThingDef>())
				{
					ThingDef partDef = source.RandomElementByWeight((ThingDef w) => w.BaseMarketValue);
					IEnumerable<RecipeDef> source2 = from x in DefDatabase<RecipeDef>.AllDefs
					where x.IsIngredient(partDef) && pawn.def.AllRecipes.Contains(x)
					select x;
					if (source2.Any<RecipeDef>())
					{
						RecipeDef recipeDef = source2.RandomElement<RecipeDef>();
						if (recipeDef.Worker.GetPartsToApplyOn(pawn, recipeDef).Any<BodyPartRecord>())
						{
							recipeDef.Worker.ApplyOnPawn(pawn, recipeDef.Worker.GetPartsToApplyOn(pawn, recipeDef).RandomElement<BodyPartRecord>(), null, emptyIngredientsList, null);
							partsMoney -= partDef.BaseMarketValue;
                            partCount++;

                        }
					}
				} else {
					partsMoney = 0.0f;
				}
			}
        	return false;
        }
    }

    class DefModExtension_AngelPawn : DefModExtension {
    	// TODO: Move to backstory and remove
        public List<string> restrictedFirstNames;
        public List<string> restrictedLastNames;

        public int maxHediffs = 1;
    }
}
