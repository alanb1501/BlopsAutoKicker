using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlopsAutoKicker
{
    internal enum ViolationType
    {
        None = 0,
        Knife,
        Perks,
        Weapon,
        Killstreaks
    }

    internal class RulesValidator
    {

        const string KnifeString = "knife_mp";
        const string BallisticKnifeString = "knife_ballistic_";

        static string[] ValidWeapons = {"dragunov_",
                                          "wa2000_",
                                          "l96a1_",
                                          "explosive_bolt_",
                                          "crossbow_explosive_",
                                          "psg1_",
                                          "hatchet_",
                                          "willy_pete",
                                          "nightingale_",
                                          "supplydrop_",
                                          "destructible_car_",
                                          "explodable_barrel_",
                                          "none"};

        internal static ViolationType CheckDamageAndKillsForViolations(string damageSource)
        {
            // check knife (cuz it's easy)
            if (damageSource.Contains(KnifeString) || damageSource.Contains(BallisticKnifeString))
            {
                return ViolationType.Knife;
            }

            //TODO: handle more granular weapon checking. Rightnow we just check for valid damage/kills, so 
            //      we don't differentiate due to perks, weapons and killstreaks.
            if (ValidWeapons.Where(a => damageSource.Contains(a)).Count() == 0)
            {
                return ViolationType.Weapon;
            }

            return ViolationType.None;
        }
    }
}
