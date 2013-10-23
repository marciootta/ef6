// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Design.VisualStudio.ModelWizard.Engine
{
    using System;
    using System.Activities;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using EnvDTE;
    using Microsoft.Data.Entity.Design.Extensibility;
    using Microsoft.Data.Entity.Design.Model;
    using Microsoft.Data.Entity.Design.Model.Database;
    using Microsoft.Data.Entity.Design.Model.Designer;
    using Microsoft.Data.Entity.Design.VersioningFacade;
    using Microsoft.Data.Entity.Design.VersioningFacade.ReverseEngineerDb;
    using Microsoft.Data.Entity.Design.VisualStudio.Package;

    internal enum ModelGenerationOption
    {
        GenerateFromDatabase = 0,
        EmptyModel = 1,
        GenerateDatabaseScript = 3,
    }

    /// <summary>
    ///     Settings class used by ModelBuilderEngine
    /// </summary>
    internal class ModelBuilderSettings
    {
        #region Constructors

        internal ModelBuilderSettings()
        {
            _applicationType = VisualStudioProjectSystem.WindowsApplication;
            UseLegacyProvider = OptionsDesignerInfo.UseLegacyProviderDefault;
        }

        #endregion Constructors

        #region Fields

        private ModelGenerationOption _modelGenerationOption = ModelGenerationOption.GenerateFromDatabase;
        private string _designTimeConnectionString;
        private string _appConfigConnectionString;
        private string _appConfigConnectionPropertyName;
        private string _designTimeProviderInvariantName;
        private string _runtimeProviderInvariantName;
        private string _modelNamespace;
        private VisualStudioProjectSystem _applicationType;
        private EFArtifact _preexistingEFArtifact;
        private readonly Dictionary<string, Object> _extensionData = new Dictionary<string, Object>();
        private WizardKind _wizardKind = WizardKind.None;
        private Dictionary<EntityStoreSchemaFilterEntry, IDataSchemaProcedure> _newFunctionSchemaProcedures;
        private Version _targetSchemaVersion;

        #endregion Fields

        #region Properties

        internal ModelGenerationOption GenerationOption
        {
            get { return _modelGenerationOption; }
            set { _modelGenerationOption = value; }
        }

        internal string InitialCatalog { get; set; }

        // virtual for testing
        internal virtual string DesignTimeConnectionString
        {
            get { return _designTimeConnectionString; }
        }

        internal string AppConfigConnectionString
        {
            get { return _appConfigConnectionString; }
        }

        internal bool SaveConnectionStringInAppConfig { get; set; }

        internal string AppConfigConnectionPropertyName
        {
            get { return _appConfigConnectionPropertyName; }
            set { _appConfigConnectionPropertyName = value; }
        }

        internal bool? IsSql9OrNewer { get; set; }

        internal ICollection<EntityStoreSchemaFilterEntry> DatabaseObjectFilters { get; set; }

        internal string ModelNamespace
        {
            get
            {
                if (String.IsNullOrEmpty(_modelNamespace))
                {
                    return ModelConstants.DefaultModelNamespace;
                }
                else
                {
                    return _modelNamespace;
                }
            }

            set { _modelNamespace = value; }
        }

        internal string StorageNamespace { get; set; }

        internal string ModelEntityContainerName
        {
            get
            {
                if (!String.IsNullOrEmpty(_appConfigConnectionPropertyName))
                {
                    return _appConfigConnectionPropertyName;
                }
                else
                {
                    return ModelConstants.DefaultEntityContainerName;
                }
            }
        }

        internal string DdlFileName { get; set; }

        internal StringReader SsdlStringReader { get; set; }

        internal StringReader MslStringReader { get; set; }

        internal StringReader DdlStringReader { get; set; }

        internal VisualStudioProjectSystem VSApplicationType
        {
            get { return _applicationType; }
            set { _applicationType = value; }
        }

        internal bool SaveToWebConfig
        {
            get
            {
                return (_applicationType == VisualStudioProjectSystem.WebApplication ||
                        _applicationType == VisualStudioProjectSystem.Website);
            }
        }

        internal WorkflowApplication WorkflowInstance { get; set; }

        internal string ProviderManifestToken { get; set; }

        /// <summary>
        ///     Returns a Dictionary mapping the identity of each _new_ Function to an IDataSchemaProcedure containing
        ///     info about the return type, params, name etc of the sproc in the database.
        ///     Any key which is mapped to a null value represents a sproc which was not processed when the
        ///     ProgressDialog was interrupted.
        /// </summary>
        internal Dictionary<EntityStoreSchemaFilterEntry, IDataSchemaProcedure> NewFunctionSchemaProcedures
        {
            get
            {
                if (null == _newFunctionSchemaProcedures)
                {
                    _newFunctionSchemaProcedures = new Dictionary<EntityStoreSchemaFilterEntry, IDataSchemaProcedure>();
                }
                return _newFunctionSchemaProcedures;
            }
        }

        /// <summary>
        ///     This will set up the design-time &amp; runtime invariant name properties &amp; connection string properties.
        /// </summary>
        /// <param name="project"></param>
        /// <param name="invariantName"></param>
        /// <param name="connectionString"></param>
        /// <param name="appConfigConnectionString"></param>
        /// <param name="isDesignTime">Indicates if invariant name &amp; connection strings are design-time or runtime.</param>
        [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1614:ElementParameterDocumentationMustHaveText")]
        internal void SetInvariantNamesAndConnectionStrings(
            Project project, string invariantName, string connectionString, string appConfigConnectionString, bool isDesignTime)
        {
            if (isDesignTime)
            {
                _designTimeConnectionString = connectionString;
                _appConfigConnectionString = ConnectionManager.TranslateConnectionString(
                    project, invariantName, appConfigConnectionString, true);
                _designTimeProviderInvariantName = invariantName;
                _runtimeProviderInvariantName = ConnectionManager.TranslateInvariantName(
                    _designTimeProviderInvariantName, _designTimeConnectionString, true);
            }
            else
            {
                _designTimeConnectionString = ConnectionManager.TranslateConnectionString(project, invariantName, connectionString, false);
                _appConfigConnectionString = appConfigConnectionString;
                _runtimeProviderInvariantName = invariantName;
                _designTimeProviderInvariantName = ConnectionManager.TranslateInvariantName(
                    _runtimeProviderInvariantName, _designTimeConnectionString, false);
            }
        }

        // this is only used by test code
        internal void SetInvariantNamesAndConnectionStrings(
            string runtimeInvariantName, string designTimeInvariantName, string connectionString, string appConfigConnectionString)
        {
            _runtimeProviderInvariantName = runtimeInvariantName;
            _designTimeProviderInvariantName = designTimeInvariantName;
            _appConfigConnectionString = appConfigConnectionString;
            _designTimeConnectionString = connectionString;
        }

        // virtual for testing
        internal virtual string RuntimeProviderInvariantName
        {
            get { return _runtimeProviderInvariantName; }
        }

        // virtual for testing
        internal virtual string DesignTimeProviderInvariantName
        {
            get { return _designTimeProviderInvariantName; }
        }

        internal TimeSpan LoadingDBMetatdataTime { get; set; }

        internal EFArtifact Artifact
        {
            get { return _preexistingEFArtifact; }
            set
            {
                Debug.Assert(_preexistingEFArtifact == null, "artifact should not be reset");

                _preexistingEFArtifact = value;
            }
        }

        internal Dictionary<string, object> ExtensionData
        {
            get { return _extensionData; }
        }

        internal T GetExtensionDataValue<T>(string keyName)
        {
            object value;
            _extensionData.TryGetValue(keyName, out value);
            return (T)value;
        }

        internal bool UsePluralizationService { get; set; }

        internal ModelBuilderEngine ModelBuilderEngine { get; set; }

        internal bool IncludeForeignKeysInModel { get; set; }

        internal WizardKind WizardKind
        {
            get { return _wizardKind; }
            set { _wizardKind = value; }
        }

        internal bool HasExtensionChangedModel { get; set; }

        public Version TargetSchemaVersion
        {
            get { return _targetSchemaVersion; }
            set
            {
                Debug.Assert(EntityFrameworkVersion.IsValidVersion(value), "value is not a valid schema version.");

                _targetSchemaVersion = value;
            }
        }

        public bool UseLegacyProvider { get; set; }

        #endregion Properties
    }
}
