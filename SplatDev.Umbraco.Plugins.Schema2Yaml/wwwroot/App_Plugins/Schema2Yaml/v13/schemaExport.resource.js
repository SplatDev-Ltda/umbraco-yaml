angular.module('umbraco.resources').factory('schemaExportResource', function ($http) {
    var baseUrl = '/umbraco/backoffice/api/SchemaExport/';

    return {
        /**
         * Runs a full export and returns { yaml, statistics }.
         */
        export: function () {
            return $http.get(baseUrl + 'Export');
        },

        /**
         * Returns statistics from the last export (no re-export).
         */
        getStatistics: function () {
            return $http.get(baseUrl + 'Statistics');
        },

        /**
         * Triggers a YAML file download via navigation (browser handles the file).
         */
        getDownloadYamlUrl: function () {
            return baseUrl + 'DownloadYaml';
        },

        /**
         * Triggers a ZIP file download via navigation (browser handles the file).
         */
        getDownloadZipUrl: function () {
            return baseUrl + 'DownloadZip';
        }
    };
});
