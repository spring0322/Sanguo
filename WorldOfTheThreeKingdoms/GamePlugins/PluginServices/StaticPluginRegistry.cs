using System;
using System.Collections.Generic;
using PluginInterface.BaseInterface;
// Namespaces for plugins
using AirViewPlugin;
using ArchitectureDetail;
using ArchitectureSurveyPlugin;
using BianduiLiebiaoChajian; 
using CommentTextPlugin;
using ConfirmationDialogPlugin;
using ContextMenuPlugin;
using CreateTroopPlugin;
using DateRunnerPlugin;
using FactionTechniquesPlugin;
using GameFormFramePlugin;
using GameRecordPlugin;
using GameSystemPlugin;
using HelpPlugin;
using InGameEditorPlugin;
using MapLayerPlugin;
using MapViewSelectorPlugin;
using MarshalSectionDialogPlugin;
using NumberInputerPlugin;
using OptionDialogPlugin;
using PersonBubble;
using PersonDetailPlugin;
using PersonPortraitPlugin;
using RoutewayEditorPlugin;
using ScreenBlindPlugin;
using SimpleTextDialogPlugin;
using TabListPlugin;
using ToolBarPlugin;
using TransportDialogPlugin;
using TreasureDetailPlugin;
using TroopDetailPlugin;
using TroopSurveyPlugin;
using TroopTitlePlugin;
using tupianwenziPlugin;
using youcelanPlugin;

namespace PluginServices
{
    public static class StaticPluginRegistry
    {
        public static void RegisterAllPlugins(PluginService service)
        {
            Register(service, new AirViewPlugin.AirViewPlugin());
            Register(service, new ArchitectureDetailPlugin());
            Register(service, new ArchitectureSurveyPlugin.ArchitectureSurveyPlugin());
            
            // BianduiLiebiaoChajian
            Register(service, new BianduiLiebiaoChajian.TabListPlugin()); 
            
            Register(service, new CommentTextPlugin.CommentTextPlugin());
            Register(service, new ConfirmationDialogPlugin.ConfirmationDialogPlugin());
            Register(service, new ContextMenuPlugin.ContextMenuPlugin());
            Register(service, new CreateTroopPlugin.CreateTroopPlugin());
            Register(service, new DateRunnerPlugin.DateRunnerPlugin());
            Register(service, new FactionTechniquesPlugin.FactionTechniquesPlugin());
            Register(service, new GameFramePlugin());
            Register(service, new GameRecordPlugin.GameRecordPlugin());
            Register(service, new GameSystemPlugin.GameSystemPlugin());
            Register(service, new HelpPlugin.HelpPlugin());
            Register(service, new InGameEditorPlugin.InGameEditorPlugin());
            Register(service, new MapLayerPlugin.MapLayerPlugin());
            Register(service, new MapViewSelectorPlugin.MapViewSelectorPlugin());
            Register(service, new MarshalSectionDialogPlugin.MarshalSectionDialogPlugin());
            Register(service, new NumberInputerPlugin.NumberInputerPlugin());
            Register(service, new OptionDialogPlugin.OptionDialogPlugin());
            Register(service, new PersonBubblePlugin());
            Register(service, new PersonDetailPlugin.PersonDetailPlugin());
            Register(service, new PersonPortraitPlugin.PersonPortraitPlugin());
            Register(service, new RoutewayEditorPlugin.RoutewayEditorPlugin());
            Register(service, new ScreenBlindPlugin.ScreenBlindPlugin());
            Register(service, new SimpleTextDialogPlugin.SimpleTextDialogPlugin());
            
            // TabListPlugin
            Register(service, new TabListPlugin.TabListPlugin());
            
            Register(service, new ToolBarPlugin.ToolBarPlugin());
            Register(service, new TransportDialogPlugin.TransportDialogPlugin());
            Register(service, new TreasureDetailPlugin.TreasureDetailPlugin());
            Register(service, new TroopDetailPlugin.TroopDetailPlugin());
            Register(service, new TroopSurveyPlugin.TroopSurveyPlugin());
            Register(service, new TroopTitlePlugin.TroopTitlePlugin());
            Register(service, new tupianwenziPlugin.tupianwenziPlugin());
            
            // youcelanPlugin
            Register(service, new youcelanPlugin.TabListPlugin());
        }

        private static void Register(PluginService service, IBasePlugin pluginInstance)
        {
            if (pluginInstance == null) return;
            
            AvailablePlugin pluginToAdd = new AvailablePlugin
            {
                AssemblyPath = "Static",
                Instance = pluginInstance
            };
            
            service.AvailablePlugins.Add(pluginToAdd);
            
            // Optional: Initialize immediately if required?
            // pluginInstance.Initialize(null); // Passing null might be dangerous if it expects a Screen.
        }
    }
}
