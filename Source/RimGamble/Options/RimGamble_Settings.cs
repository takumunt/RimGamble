using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;

namespace RimGamble.Options
{
    public class RimGamble_Settings : ModSettings
    {
        // Existing Ricky Risk settings
        public const int bigEventMtbBase = 3;
        public static int bigEventMtb = bigEventMtbBase;

        // Traveling Gambler settings
        public static bool enableTravelingGambler = true;
        public static int gamblerSpawnFrequency = 60;
        public static int gamblerMinStayDuration = 3;
        public static int gamblerMaxStayDuration = 7;
        public static float gamblerNegativeOutcomeChance = 0.3f;
        public static bool allowGamblerViolence = true;
        public static bool allowGamblerTheft = true;
        public static int gamblerMinWealth = 1000;
        public static bool showGamblerWarnings = true;

        // Behavior Categories
        public static bool enableAcceptanceBehaviors = true;
        public static bool enableAggressiveBehaviors = true;
        public static bool enableRejectionBehaviors = true;
        
        // Specific Acceptance Behaviors
        public static bool allowPeacefulDeparture = true;
        public static bool allowHumanBomb = true;
        public static bool allowSabotage = true;
        public static bool allowPartyMood = true;
        public static bool allowRumorSpread = true;
        public static bool allowTheftAccept = true;
        public static bool allowTradeCaravan = true;
        public static bool allowDropPod = true;
        public static bool allowTeachSkill = true;
        public static bool allowGiveInspiration = true;
        public static bool allowAskJoin = true;
        
        // Specific Aggressive Behaviors
        public static bool allowAssault = true;
        public static bool allowRaid = true;
        public static bool allowDelayedRaid = true;
        public static bool allowTheft = true;
        
        // Specific Rejection Behaviors
        public static bool allowPeacefulRejection = true;
        public static bool allowAggressiveRejection = true;

        private static int currentTab = 0;
        private static readonly string[] tabLabels = { "General", "Behaviors" };
        private static Vector2 scrollPosition = Vector2.zero;

        public override void ExposeData()
        {
            base.ExposeData();
            
            Scribe_Values.Look(ref bigEventMtb, "bigEventMtb", bigEventMtbBase, true);
            Scribe_Values.Look(ref enableTravelingGambler, "enableTravelingGambler", true);
            Scribe_Values.Look(ref gamblerSpawnFrequency, "gamblerSpawnFrequency", 60);
            Scribe_Values.Look(ref gamblerMinStayDuration, "gamblerMinStayDuration", 3);
            Scribe_Values.Look(ref gamblerMaxStayDuration, "gamblerMaxStayDuration", 7);
            Scribe_Values.Look(ref gamblerNegativeOutcomeChance, "gamblerNegativeOutcomeChance", 0.3f);
            Scribe_Values.Look(ref allowGamblerViolence, "allowGamblerViolence", true);
            Scribe_Values.Look(ref allowGamblerTheft, "allowGamblerTheft", true);
            Scribe_Values.Look(ref gamblerMinWealth, "gamblerMinWealth", 1000);
            Scribe_Values.Look(ref showGamblerWarnings, "showGamblerWarnings", true);
            
            Scribe_Values.Look(ref enableAcceptanceBehaviors, "enableAcceptanceBehaviors", true);
            Scribe_Values.Look(ref enableAggressiveBehaviors, "enableAggressiveBehaviors", true);
            Scribe_Values.Look(ref enableRejectionBehaviors, "enableRejectionBehaviors", true);
            
            Scribe_Values.Look(ref allowPeacefulDeparture, "allowPeacefulDeparture", true);
            Scribe_Values.Look(ref allowHumanBomb, "allowHumanBomb", true);
            Scribe_Values.Look(ref allowSabotage, "allowSabotage", true);
            Scribe_Values.Look(ref allowPartyMood, "allowPartyMood", true);
            Scribe_Values.Look(ref allowRumorSpread, "allowRumorSpread", true);
            Scribe_Values.Look(ref allowTheftAccept, "allowTheftAccept", true);
            Scribe_Values.Look(ref allowTradeCaravan, "allowTradeCaravan", true);
            Scribe_Values.Look(ref allowDropPod, "allowDropPod", true);
            Scribe_Values.Look(ref allowTeachSkill, "allowTeachSkill", true);
            Scribe_Values.Look(ref allowGiveInspiration, "allowGiveInspiration", true);
            Scribe_Values.Look(ref allowAskJoin, "allowAskJoin", true);
            
            Scribe_Values.Look(ref allowAssault, "allowAssault", true);
            Scribe_Values.Look(ref allowRaid, "allowRaid", true);
            Scribe_Values.Look(ref allowDelayedRaid, "allowDelayedRaid", true);
            Scribe_Values.Look(ref allowTheft, "allowTheft", true);
            
            Scribe_Values.Look(ref allowPeacefulRejection, "allowPeacefulRejection", true);
            Scribe_Values.Look(ref allowAggressiveRejection, "allowAggressiveRejection", true);
        }

        public static void DoWindowContents(Rect inRect)
        {
            // Add reset button in top right
            var resetButtonWidth = 120f;
            var resetButtonHeight = 30f;
            var resetButtonRect = new Rect(inRect.width - resetButtonWidth - 10f, 5f, resetButtonWidth, resetButtonHeight);
            
            if (Widgets.ButtonText(resetButtonRect, "🔄 Reset All"))
            {
                ResetAllSettings();
                Messages.Message("All RimGamble settings reset to defaults!", MessageTypeDefOf.PositiveEvent);
            }
            
            // Push everything down to avoid overlap with default title and reset button
            var titleHeight = 40f;
            var tabRect = new Rect(inRect.x, inRect.y + titleHeight, inRect.width, 30f);
            var contentRect = new Rect(inRect.x, inRect.y + titleHeight + 35f, inRect.width, inRect.height - titleHeight - 35f);
            
            List<TabRecord> tabs = new List<TabRecord>();
            for (int i = 0; i < tabLabels.Length; i++)
            {
                int tabIndex = i;
                tabs.Add(new TabRecord(tabLabels[i], () => currentTab = tabIndex, currentTab == i));
            }
            TabDrawer.DrawTabs(tabRect, tabs);
            
            // Add scrollable view
            var viewRect = new Rect(0f, 0f, contentRect.width - 16f, GetContentHeight());
            Widgets.BeginScrollView(contentRect, ref scrollPosition, viewRect);
            
            Listing_Standard ls = new Listing_Standard();
            ls.Begin(viewRect);

            switch (currentTab)
            {
                case 0:
                    DrawGeneralTab(ls);
                    break;
                case 1:
                    DrawBehaviorsTab(ls);
                    break;
            }

            ls.End();
            Widgets.EndScrollView();
        }

        private static float GetContentHeight()
        {
            // Calculate approximate content height for scrolling
            switch (currentTab)
            {
                case 0: return enableTravelingGambler ? 400f : 200f; // General tab
                case 1: return enableTravelingGambler ? 800f : 100f; // Behaviors tab
                default: return 400f;
            }
        }

        private static void DrawGeneralTab(Listing_Standard ls)
        {
            DrawSectionHeader(ls, "Ricky Risk Storyteller");
            ls.Label("Big Event Frequency: " + bigEventMtb + " days");
            bigEventMtb = (int)ls.Slider(bigEventMtb, 1, 30);
            ls.Gap(12f);

            DrawSectionHeader(ls, "Traveling Gambler");
            ls.CheckboxLabeled("Enable Traveling Gambler", ref enableTravelingGambler);
            
            if (enableTravelingGambler)
            {
                ls.Gap(6f);
                ls.Label("Spawn Frequency: " + gamblerSpawnFrequency + " days");
                gamblerSpawnFrequency = (int)ls.Slider(gamblerSpawnFrequency, 15, 120);
                
                ls.Label("Stay Duration: " + gamblerMinStayDuration + "-" + gamblerMaxStayDuration + " days");
                gamblerMinStayDuration = (int)ls.Slider(gamblerMinStayDuration, 1, 10);
                gamblerMaxStayDuration = Mathf.Max(gamblerMinStayDuration, (int)ls.Slider(gamblerMaxStayDuration, gamblerMinStayDuration, 15));
                
                ls.Label("Minimum Colony Wealth: " + gamblerMinWealth.ToString("N0"));
                gamblerMinWealth = (int)ls.Slider(gamblerMinWealth, 0, 10000);
            }
        }

        private static void DrawBehaviorsTab(Listing_Standard ls)
        {
            if (!enableTravelingGambler)
            {
                Text.Font = GameFont.Medium;
                ls.Label("Enable traveling gamblers in the General tab to configure behaviors.");
                Text.Font = GameFont.Small;
                return;
            }

            Text.Font = GameFont.Medium;
            ls.Label("Gambler Behavior Configuration");
            Text.Font = GameFont.Small;
            ls.Gap(8f);

            DrawSectionHeader(ls, "Acceptance Behaviors");
            ls.Label("What happens when you accept the gambler into your colony:");
            ls.Gap(8f);
            
            DrawIndentedCheckbox(ls, "Peaceful departure", ref allowPeacefulDeparture);
            DrawIndentedCheckbox(ls, "Human bomb incident", ref allowHumanBomb);
            DrawIndentedCheckbox(ls, "Sabotage incident", ref allowSabotage);
            DrawIndentedCheckbox(ls, "Party mood boost", ref allowPartyMood);
            DrawIndentedCheckbox(ls, "Rumor spread", ref allowRumorSpread);
            DrawIndentedCheckbox(ls, "Theft incident (acceptance)", ref allowTheftAccept);
            DrawIndentedCheckbox(ls, "Trade caravan arrival", ref allowTradeCaravan);
            DrawIndentedCheckbox(ls, "Drop pod delivery", ref allowDropPod);
            DrawIndentedCheckbox(ls, "Teach skill", ref allowTeachSkill);
            DrawIndentedCheckbox(ls, "Give inspiration", ref allowGiveInspiration);
            DrawIndentedCheckbox(ls, "Ask to join colony", ref allowAskJoin);
            
            ls.Gap(12f);

            DrawSectionHeader(ls, "Aggressive Behaviors");
            ls.Label("Violent and theft outcomes during gambling:");
            ls.Gap(8f);
            
            // Prevent disabling all aggressive behaviors
            bool isLastAssault = IsLastAggressiveBehavior("Assault");
            bool isLastRaid = IsLastAggressiveBehavior("Raid");
            bool isLastDelayedRaid = IsLastAggressiveBehavior("DelayedRaid");
            bool isLastTheft = IsLastAggressiveBehavior("Theft");
            
            DrawIndentedCheckbox(ls, "Direct assault" + (isLastAssault ? " (at least one required)" : ""), ref allowAssault, isLastAssault);
            DrawIndentedCheckbox(ls, "Immediate raid" + (isLastRaid ? " (at least one required)" : ""), ref allowRaid, isLastRaid);
            DrawIndentedCheckbox(ls, "Delayed raid" + (isLastDelayedRaid ? " (at least one required)" : ""), ref allowDelayedRaid, isLastDelayedRaid);
            DrawIndentedCheckbox(ls, "Theft incident" + (isLastTheft ? " (at least one required)" : ""), ref allowTheft, isLastTheft);
            
            ls.Gap(12f);

            DrawSectionHeader(ls, "Rejection Behaviors");
            ls.Label("What happens when you reject the gambler:");
            ls.Gap(8f);
            
            // Prevent disabling all rejection behaviors
            bool isLastPeaceful = IsLastRejectionBehavior("Peaceful");
            bool isLastAggressive = IsLastRejectionBehavior("Aggressive");
            
            DrawIndentedCheckbox(ls, "Peaceful departure" + (isLastPeaceful ? " (at least one required)" : ""), ref allowPeacefulRejection, isLastPeaceful);
            DrawIndentedCheckbox(ls, "Aggressive response" + (isLastAggressive ? " (at least one required)" : ""), ref allowAggressiveRejection, isLastAggressive);
        }

        private static void DrawSectionHeader(Listing_Standard ls, string text)
        {
            var prevFont = Text.Font;
            var prevColor = GUI.color;
            
            ls.Gap(6f);
            Text.Font = GameFont.Medium;
            GUI.color = new Color(0.8f, 0.8f, 1f);
            ls.Label(text);
            
            var rect = ls.GetRect(2f);
            GUI.color = new Color(0.6f, 0.6f, 0.8f, 0.5f);
            Widgets.DrawLineHorizontal(rect.x, rect.y, rect.width);
            
            Text.Font = prevFont;
            GUI.color = prevColor;
            ls.Gap(6f);
        }

        private static void DrawSubsectionHeader(Listing_Standard ls, string text)
        {
            var prevColor = GUI.color;
            ls.Gap(4f);
            GUI.color = new Color(0.9f, 0.9f, 0.9f);
            ls.Label("  • " + text);
            GUI.color = prevColor;
            ls.Gap(4f);
        }

        private static void DrawIndentedCheckbox(Listing_Standard ls, string label, ref bool value)
        {
            var rect = ls.GetRect(Text.LineHeight + 2f);
            rect.x += 40f;
            rect.width -= 40f;
            
            if (Mouse.IsOver(rect))
            {
                var bgRect = new Rect(rect.x - 5f, rect.y, rect.width + 10f, rect.height);
                GUI.color = new Color(1f, 1f, 1f, 0.1f);
                Widgets.DrawHighlight(bgRect);
                GUI.color = Color.white;
            }
            
            // Check if this would be the last acceptance behavior being disabled
            bool wouldBeLastAcceptance = IsLastAcceptanceBehavior(ref value);
            
            if (wouldBeLastAcceptance && value)
            {
                // Don't allow disabling the last acceptance behavior - enforce by preventing the change
                GUI.color = Color.gray;
                bool tempValue = value; // Create temp to prevent changing the actual value
                Widgets.CheckboxLabeled(rect, label + " (at least one required)", ref tempValue);
                GUI.color = Color.white;
                
                if (Widgets.ButtonInvisible(rect))
                {
                    Messages.Message("At least one acceptance behavior must remain enabled!", MessageTypeDefOf.RejectInput);
                }
                // Don't change the actual value - keep it enabled
            }
            else
            {
                bool oldValue = value;
                Widgets.CheckboxLabeled(rect, label, ref value);
                
                // If they disabled this and it would leave none enabled, auto-enable the first one
                if (oldValue && !value && GetEnabledAcceptanceBehaviors().Count == 0)
                {
                    allowPeacefulDeparture = true; // Auto-enable first option
                }
            }
        }
        
        /// <summary>
        /// Draw indented checkbox with custom validation logic
        /// </summary>
        private static void DrawIndentedCheckbox(Listing_Standard ls, string label, ref bool value, bool isLastOfType)
        {
            var rect = ls.GetRect(Text.LineHeight + 2f);
            rect.x += 40f;
            rect.width -= 40f;
            
            if (Mouse.IsOver(rect))
            {
                var bgRect = new Rect(rect.x - 5f, rect.y, rect.width + 10f, rect.height);
                GUI.color = new Color(1f, 1f, 1f, 0.1f);
                Widgets.DrawHighlight(bgRect);
                GUI.color = Color.white;
            }
            
            if (isLastOfType && value)
            {
                // Don't allow disabling the last behavior of this type - enforce by preventing the change
                GUI.color = Color.gray;
                bool tempValue = value; // Create temp to prevent changing the actual value
                Widgets.CheckboxLabeled(rect, label, ref tempValue);
                GUI.color = Color.white;
                
                if (Widgets.ButtonInvisible(rect))
                {
                    Messages.Message("At least one behavior of this type must remain enabled!", MessageTypeDefOf.RejectInput);
                }
                // Don't change the actual value - keep it enabled
            }
            else
            {
                bool oldValue = value;
                Widgets.CheckboxLabeled(rect, label, ref value);
                
                // Auto-enforcement logic: if they disabled this and it would leave none enabled, auto-enable the first one
                if (oldValue && !value)
                {
                    // Check if this was for aggressive behaviors
                    if (enableAggressiveBehaviors && GetEnabledAggressiveBehaviors().Count == 0)
                    {
                        allowAssault = true; // Auto-enable first aggressive option
                    }
                    // Check if this was for rejection behaviors
                    else if (enableRejectionBehaviors && GetEnabledRejectionBehaviors().Count == 0)
                    {
                        allowPeacefulRejection = true; // Auto-enable first rejection option
                    }
                }
            }
        }
        
        private static bool IsLastAcceptanceBehavior(ref bool currentValue)
        {
            if (currentValue || !enableAcceptanceBehaviors) return false;
            
            // Count how many acceptance behaviors are currently enabled
            int enabledCount = 0;
            if (allowPeacefulDeparture) enabledCount++;
            if (allowHumanBomb) enabledCount++;
            if (allowSabotage) enabledCount++;
            if (allowPartyMood) enabledCount++;
            if (allowRumorSpread) enabledCount++;
            if (allowTheftAccept) enabledCount++;
            if (allowTradeCaravan) enabledCount++;
            if (allowDropPod) enabledCount++;
            if (allowTeachSkill) enabledCount++;
            if (allowGiveInspiration) enabledCount++;
            if (allowAskJoin) enabledCount++;
            
            return enabledCount <= 1; // Would be the last one if disabled
        }
        
        public static List<string> GetEnabledAcceptanceBehaviors()
        {
            var behaviors = new List<string>();
            if (!enableAcceptanceBehaviors) return behaviors;
            
            if (allowPeacefulDeparture) behaviors.Add("Peaceful Departure");
            if (allowHumanBomb) behaviors.Add("Human Bomb");
            if (allowSabotage) behaviors.Add("Sabotage");
            if (allowPartyMood) behaviors.Add("Party Mood");
            if (allowRumorSpread) behaviors.Add("Rumor Spread");
            if (allowTheftAccept) behaviors.Add("Theft");
            if (allowTradeCaravan) behaviors.Add("Trade Caravan");
            if (allowDropPod) behaviors.Add("Drop Pod");
            if (allowTeachSkill) behaviors.Add("Teach Skill");
            if (allowGiveInspiration) behaviors.Add("Give Inspiration");
            if (allowAskJoin) behaviors.Add("Ask Join");
            
            return behaviors;
        }
        
        public static List<string> GetDisabledAcceptanceBehaviors()
        {
            var behaviors = new List<string>();
            if (!enableAcceptanceBehaviors) return new List<string> { "All acceptance behaviors disabled" };
            
            if (!allowPeacefulDeparture) behaviors.Add("Peaceful Departure");
            if (!allowHumanBomb) behaviors.Add("Human Bomb");
            if (!allowSabotage) behaviors.Add("Sabotage");
            if (!allowPartyMood) behaviors.Add("Party Mood");
            if (!allowRumorSpread) behaviors.Add("Rumor Spread");
            if (!allowTheftAccept) behaviors.Add("Theft");
            if (!allowTradeCaravan) behaviors.Add("Trade Caravan");
            if (!allowDropPod) behaviors.Add("Drop Pod");
            if (!allowTeachSkill) behaviors.Add("Teach Skill");
            if (!allowGiveInspiration) behaviors.Add("Give Inspiration");
            if (!allowAskJoin) behaviors.Add("Ask Join");
            
            return behaviors;
        }
        
        /// <summary>
        /// Check if disabling this aggressive behavior would leave none enabled
        /// </summary>
        private static bool IsLastAggressiveBehavior(string behaviorName)
        {
            if (!enableAggressiveBehaviors) return false;
            
            int enabledCount = 0;
            if (allowAssault) enabledCount++;
            if (allowRaid) enabledCount++;
            if (allowDelayedRaid) enabledCount++;
            if (allowTheft) enabledCount++;
            
            return enabledCount <= 1; // Would be the last one if disabled
        }
        
        /// <summary>
        /// Check if disabling this rejection behavior would leave none enabled
        /// </summary>
        private static bool IsLastRejectionBehavior(string behaviorName)
        {
            if (!enableRejectionBehaviors) return false;
            
            int enabledCount = 0;
            if (allowPeacefulRejection) enabledCount++;
            if (allowAggressiveRejection) enabledCount++;
            
            return enabledCount <= 1; // Would be the last one if disabled
        }
        
        public static List<string> GetEnabledAggressiveBehaviors()
        {
            var behaviors = new List<string>();
            if (!enableAggressiveBehaviors) return behaviors;
            
            if (allowAssault) behaviors.Add("Assault");
            if (allowRaid) behaviors.Add("Raid");
            if (allowDelayedRaid) behaviors.Add("Delayed Raid");
            if (allowTheft) behaviors.Add("Theft");
            
            return behaviors;
        }
        
        public static List<string> GetEnabledRejectionBehaviors()
        {
            var behaviors = new List<string>();
            if (!enableRejectionBehaviors) return behaviors;
            
            if (allowPeacefulRejection) behaviors.Add("Peaceful Departure");
            if (allowAggressiveRejection) behaviors.Add("Aggressive Response");
            
            return behaviors;
        }
        
        /// <summary>
        /// Reset all settings to their default values
        /// </summary>
        public static void ResetAllSettings()
        {
            // Ricky Risk settings
            bigEventMtb = bigEventMtbBase;
            
            // Traveling Gambler general settings
            enableTravelingGambler = true;
            gamblerSpawnFrequency = 60;
            gamblerMinStayDuration = 3;
            gamblerMaxStayDuration = 7;
            gamblerNegativeOutcomeChance = 0.3f;
            allowGamblerViolence = true;
            allowGamblerTheft = true;
            gamblerMinWealth = 1000;
            showGamblerWarnings = true;
            
            // Behavior Categories
            enableAcceptanceBehaviors = true;
            enableAggressiveBehaviors = true;
            enableRejectionBehaviors = true;
            
            // Acceptance Behaviors
            allowPeacefulDeparture = true;
            allowHumanBomb = true;
            allowSabotage = true;
            allowPartyMood = true;
            allowRumorSpread = true;
            allowTheftAccept = true;
            allowTradeCaravan = true;
            allowDropPod = true;
            allowTeachSkill = true;
            allowGiveInspiration = true;
            allowAskJoin = true;
            
            // Aggressive Behaviors
            allowAssault = true;
            allowRaid = true;
            allowDelayedRaid = true;
            allowTheft = true;
            
            // Rejection Behaviors
            allowPeacefulRejection = true;
            allowAggressiveRejection = true;
        }
    }
}
