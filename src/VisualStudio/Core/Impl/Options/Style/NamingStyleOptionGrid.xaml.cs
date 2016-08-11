﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Controls;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.VisualStudio.LanguageServices.Implementation.Options.Style.NamingPreferences;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options.Style
{
    internal partial class NamingStyleOptionGrid : AbstractOptionPageControl
    {
        private readonly IEnumerable<SymbolSpecification> _specifications = WellKnownNamingInfo.GetWellKnownSymbolSpecifications();
        private readonly IEnumerable<NamingStyle> _styles = WellKnownNamingInfo.GetWellKnownNamingStyles();
        private readonly EnforcementLevel[] _notifications = new[]
                {
                    new EnforcementLevel(DiagnosticSeverity.Hidden),
                    new EnforcementLevel(DiagnosticSeverity.Info),
                    new EnforcementLevel(DiagnosticSeverity.Warning),
                    new EnforcementLevel(DiagnosticSeverity.Error),
                };
        private readonly string _languageName;
        private readonly ViewModel _items = new ViewModel();

        internal NamingStyleOptionGrid(IServiceProvider serviceProvider, string languageName)
            : base(serviceProvider)
        {
            _languageName = languageName;
            this.DataContext = _items;
            InitializeComponent();
            CodeStyleMembers.SelectionChanged += CodeStyleMembers_SelectionChanged;
        }

        private void CodeStyleMembers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private ItemViewModel CreateItemWithNoSelections()
        {
            return new ItemViewModel()
            {
                Specifications = _specifications,
                NamingStyles = _styles,
                NotificationPreferences = _notifications
            };
        }

        private void AddButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            _items.CodeStyleItems.Add(CreateItemWithNoSelections());
        }

        internal override void SaveSettings()
        {
            var info = new SerializableNamingStylePreferencesInfo();

            foreach (var item in _items.CodeStyleItems)
            {
                if (!item.IsComplete())
                {
                    continue;
                }

                var rule = new SerializableNamingRule()
                {
                    EnforcementLevel = item.SelectedNotificationPreference.Value,
                    NamingStyleID = item.SelectedStyle.ID,
                    SymbolSpecificationID = item.SelectedSpecification.ID
                };

                info.NamingRules.Add(rule);
            }

            var oldOptions = OptionService.GetOptions();
            var newOptions = oldOptions.WithChangedOption(SimplificationOptions.NamingPreferences, _languageName, info.CreateXElement().ToString());

            OptionService.SetOptions(newOptions);
            OptionLogger.Log(oldOptions, newOptions);
        }

        internal override void LoadSettings()
        {
            var options = OptionService.GetOption(SimplificationOptions.NamingPreferences, _languageName);
            if (string.IsNullOrEmpty(options))
            {
                return;
            }

            var info = SerializableNamingStylePreferencesInfo.FromXElement(XElement.Parse(options));

            _items.CodeStyleItems.Clear();
            foreach (var namingRule in info.NamingRules)
            {
                var item = CreateItemWithNoSelections();
                item.SelectedStyle = item.NamingStyles.First(n => n.ID == namingRule.NamingStyleID);
                item.SelectedSpecification = item.Specifications.First(s => s.ID == namingRule.SymbolSpecificationID);
                item.SelectedNotificationPreference = item.NotificationPreferences.First(n => n.Value == namingRule.EnforcementLevel);
                _items.CodeStyleItems.Add(item);
            }
        }

        internal bool ContainsErrors()
        {
            return _items.CodeStyleItems.Any(i => !i.IsComplete());
        }

        private void RemoveButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var button = (Button)sender;
            var context = button.DataContext as ItemViewModel;
            _items.CodeStyleItems.Remove(context);
        }

        private class ViewModel
        {
            public ViewModel()
            {
                CodeStyleItems = new ObservableCollection<ItemViewModel>();
            }

            public ObservableCollection<ItemViewModel> CodeStyleItems { get; set; }
        }
    }
}