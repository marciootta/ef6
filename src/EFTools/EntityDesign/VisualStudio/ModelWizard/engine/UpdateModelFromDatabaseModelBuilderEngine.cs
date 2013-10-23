﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Design.VisualStudio.ModelWizard.Engine
{
    using System;

    internal class UpdateModelFromDatabaseModelBuilderEngine : InMemoryModelBuilderEngine
    {
        internal UpdateModelFromDatabaseModelBuilderEngine(
            ModelBuilderEngineHostContext hostContext, ModelBuilderSettings settings, Uri uri)
            :
                base(hostContext, settings, new InitialModelContentsFactory(), uri)
        {
        }

        protected override void UpdateDesignerInfo(EdmxHelper edmxHelper)
        {
            // in the update model case, there is no need to fix up the designer info
            // since it would only be applied to the temporary artifact and then ignored
        }
    }
}
