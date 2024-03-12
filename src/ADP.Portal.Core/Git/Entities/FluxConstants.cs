﻿namespace ADP.Portal.Core.Git.Entities
{
    internal class FluxConstants
    {
        public const string POSTGRES_DB = "POSTGRES_DB";
        public const string SERVICE_FOLDER = "flux/templates/programme/team/service";
        public const string ENVIRONMENT_FOLDER = "flux/templates/programme/team/environment";
        public const string PRE_DEPLOY_FOLDER = "flux/templates/programme/team/service/pre-deploy";
        public const string PRE_DEPLOY_KUSTOMIZE_FILE = "flux/templates/programme/team/service/pre-deploy-kustomize.yaml";
        public const string TEAM_ENV_FOLDER = "flux/templates/programme/team/environment";
        public const string TEAM_ENV_KUSTOMIZATION_FILE = "flux/templates/programme/team/environment/kustomization.yaml";
    }
}
