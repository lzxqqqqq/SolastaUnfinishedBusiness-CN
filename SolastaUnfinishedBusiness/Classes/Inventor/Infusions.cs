﻿using System.Linq;
using SolastaUnfinishedBusiness.Builders;
using SolastaUnfinishedBusiness.Builders.Features;
using SolastaUnfinishedBusiness.CustomBehaviors;
using SolastaUnfinishedBusiness.CustomDefinitions;
using UnityEngine.AddressableAssets;
using static FeatureDefinitionAttributeModifier;
using static RuleDefinitions;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper;

namespace SolastaUnfinishedBusiness.Classes.Inventor;

internal static class Infusions
{
    private const string ReplicaItemTitleFormat = $"Item/&ReplicaItemFormatTitle";
    private const string ReplicaItemTitleDescription = $"Item/&ReplicaItemFormatDescription";

    public static void Build()
    {
        var name = "InfusionEnhanceArcaneFocus";
        BuildInfuseItemPowerInvocation(2, name,
            FeatureDefinitionPowers.PowerDomainOblivionHeraldOfPain.GuiPresentation.SpriteReference, IsFocusOrStaff,
            FeatureDefinitionMagicAffinityBuilder
                //TODO: RAW needs to require attunement
                .Create($"MagicAffinity{name}")
                .SetGuiPresentation(name, Category.Feature, FeatureDefinitionAttackModifiers.AttackModifierMagicWeapon3)
                .SetCastingModifiers(1, dcModifier: 1)
                .AddToDB());

        name = "InfusionEnhanceDefense";
        BuildInfuseItemPowerInvocation(2, name,
            SpellDefinitions.MageArmor.GuiPresentation.SpriteReference, IsArmor,
            FeatureDefinitionAttributeModifierBuilder
                .Create($"AttributeModifier{name}")
                .SetGuiPresentation(name, Category.Feature, FeatureDefinitionAttackModifiers.AttackModifierMagicWeapon3)
                .SetModifier(AttributeModifierOperation.Additive, AttributeDefinitions.ArmorClass, 1)
                .AddToDB());

        name = "InfusionEnhanceWeapon";
        BuildInfuseItemPowerInvocation(2, name,
            SpellDefinitions.MagicWeapon.GuiPresentation.SpriteReference, IsWeapon,
            FeatureDefinitionAttackModifierBuilder
                .Create($"AttackModifier{name}")
                .SetGuiPresentation(name, Category.Feature, FeatureDefinitionAttackModifiers.AttackModifierMagicWeapon3)
                .Configure(attackRollModifier: 1, damageRollModifier: 1)
                .AddToDB());

        BuildCreateItemPowerInvocation(ItemDefinitions.Backpack_Bag_Of_Holding);
        BuildCreateItemPowerInvocation(ItemDefinitions.WandOfMagicDetection);
        BuildCreateItemPowerInvocation(ItemDefinitions.WandOfIdentify);
    }

    private static void BuildInfuseItemPowerInvocation(int level, string name, AssetReferenceSprite icon,
        IsValidItemHandler filter, params FeatureDefinition[] features)
    {
        CustomInvocationDefinitionBuilder
            .Create($"Invocation{name}")
            .SetGuiPresentation(name, Category.Feature, icon)
            .SetPoolType(CustomInvocationPoolType.Pools.Infusion)
            .SetRequiredLevel(level)
            .SetGrantedFeature(BuildInfuseItemPower(name, icon, filter, features))
            .AddToDB();
    }

    private static void BuildCreateItemPowerInvocation(ItemDefinition item, int level = 2)
    {
        var replica = BuildItemReplica(item);
        var description = BuildReplicaDescription(item);
        var invocation = CustomInvocationDefinitionBuilder
            .Create($"InvocationCreate{replica.name}")
            .SetGuiPresentation(Category.Feature, replica)
            .SetPoolType(CustomInvocationPoolType.Pools.Infusion)
            .SetRequiredLevel(level)
            .SetGrantedFeature(BuildCreateItemPower(replica, description))
            .AddToDB();

        invocation.Item = replica;
        invocation.GuiPresentation.title = replica.FormatTitle();
        invocation.GuiPresentation.description = description;
    }

    private static FeatureDefinitionPowerSharedPool BuildInfuseItemPower(string name,
        AssetReferenceSprite icon, IsValidItemHandler itemFilter, params FeatureDefinition[] features)
    {
        return FeatureDefinitionPowerSharedPoolBuilder.Create($"Power{name}")
            .SetGuiPresentation(name, Category.Feature, icon)
            .SetActivationTime(ActivationTime.Action)
            .SetCostPerUse(1)
            .SetUniqueInstance()
            .SetSharedPool(InventorClass.InfusionPool)
            .SetCustomSubFeatures(ExtraCarefulTrackedItem.Marker,
                InventorClass.InfusionLimiter,
                new CustomItemFilter(itemFilter))
            .SetEffectDescription(new EffectDescriptionBuilder()
                .SetAnimation(AnimationDefinitions.AnimationMagicEffect.Animation1)
                .SetTargetingData(Side.Ally, RangeType.Self, 1, TargetType.Item,
                    itemSelectionType: ActionDefinitions.ItemSelectionType.Carried)
                .SetParticleEffectParameters(FeatureDefinitionPowers.PowerOathOfJugementWeightOfJustice)
                .SetDurationData(DurationType.Permanent)
                .SetEffectForms(new EffectFormBuilder()
                    .HasSavingThrow(EffectSavingThrowType.None)
                    .SetItemPropertyForm(features.Select(f => new FeatureUnlockByLevel(f, 0)),
                        ItemPropertyUsage.Unlimited, 1)
                    .Build())
                .Build())
            .AddToDB();
    }

    private static FeatureDefinitionPowerSharedPool BuildCreateItemPower(ItemDefinition item, string description)
    {
        var power = FeatureDefinitionPowerSharedPoolBuilder.Create($"PowerCreate{item.name}")
            .SetGuiPresentation(Category.Feature, item)
            .SetActivationTime(ActivationTime.Action)
            .SetCostPerUse(1)
            .SetUniqueInstance()
            .SetSharedPool(InventorClass.InfusionPool)
            .SetCustomSubFeatures(ExtraCarefulTrackedItem.Marker, InventorClass.InfusionLimiter)
            .SetEffectDescription(new EffectDescriptionBuilder()
                .SetAnimation(AnimationDefinitions.AnimationMagicEffect.Animation1)
                .SetTargetingData(Side.All, RangeType.Self, 1, TargetType.Self)
                .SetParticleEffectParameters(SpellDefinitions.Bless)
                .SetDurationData(DurationType.Permanent)
                .SetEffectForms(new EffectFormBuilder()
                    .HasSavingThrow(EffectSavingThrowType.None)
                    .SetSummonItemForm(item, 1, true)
                    .Build())
                .Build())
            .AddToDB();

        power.GuiPresentation.title = item.FormatTitle();
        power.GuiPresentation.description = description;

        return power;
    }

    private static ItemDefinition BuildItemReplica(ItemDefinition baseItem)
    {
        var replica = ItemDefinitionBuilder
            .Create(baseItem, $"InfusedReplica{baseItem.name}")
            .AddItemTags(TagsDefinitions.ItemTagQuest) //TODO: implement custon tag, instead of quest
            .SetGold(0)
            .AddToDB();

        replica.GuiPresentation.title = GuiReplicaTitle(baseItem);

        return replica;
    }

    private static string GuiReplicaTitle(ItemDefinition item)
    {
        return Gui.Format(ReplicaItemTitleFormat, item.FormatTitle());
    }

    private static string BuildReplicaDescription(ItemDefinition item)
    {
        return Gui.Format(ReplicaItemTitleDescription, item.FormatTitle(), item.FormatDescription());
    }

    #region Item Filters

    private static bool IsFocusOrStaff(RulesetCharacter _, RulesetItem item)
    {
        var definition = item.ItemDefinition;
        var staffType = WeaponTypeDefinitions.QuarterstaffType.Name;
        return !definition.Magical
               && (definition.IsFocusItem
                   || (definition.IsWeapon && definition.WeaponDescription.WeaponType == staffType));
    }

    private static bool IsWeapon(RulesetCharacter _, RulesetItem item)
    {
        var definition = item.ItemDefinition;
        return !definition.Magical && definition.IsWeapon;
    }

    private static bool IsArmor(RulesetCharacter _, RulesetItem item)
    {
        var definition = item.ItemDefinition;
        return !definition.Magical && definition.IsArmor;
    }

    #endregion
}
