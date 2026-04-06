angular.module('umbraco').controller('Schema2Yaml.ExportController', [
    '$scope',
    'schemaExportResource',
    'notificationsService',
    function ($scope, schemaExportResource, notificationsService) {

        // ─── State ────────────────────────────────────────────────────────────

        $scope.loading = false;
        $scope.downloadingZip = false;
        $scope.hasExport = false;
        $scope.yaml = null;
        $scope.stats = null;
        $scope.yamlPreview = null;
        $scope.previewTruncated = false;

        var PREVIEW_LIMIT = 10000;

        // ─── Helpers ──────────────────────────────────────────────────────────

        function applyStats(statistics) {
            $scope.stats = statistics;
        }

        function applyYaml(yaml) {
            $scope.yaml = yaml;
            $scope.hasExport = true;

            if (yaml && yaml.length > PREVIEW_LIMIT) {
                $scope.yamlPreview = yaml.substring(0, PREVIEW_LIMIT);
                $scope.previewTruncated = true;
            } else {
                $scope.yamlPreview = yaml;
                $scope.previewTruncated = false;
            }
        }

        // ─── Actions ──────────────────────────────────────────────────────────

        /**
         * Runs a full export and populates stats + YAML preview.
         */
        $scope.runExport = function () {
            if ($scope.loading) { return; }

            $scope.loading = true;
            $scope.hasExport = false;
            $scope.yaml = null;
            $scope.stats = null;
            $scope.yamlPreview = null;

            schemaExportResource.export()
                .then(function (response) {
                    var data = response.data;
                    applyStats(data.statistics);
                    applyYaml(data.yaml);
                    notificationsService.success('Schema Export', 'Export completed successfully.');
                })
                .catch(function (error) {
                    var msg = (error.data && error.data.message) ? error.data.message : 'An unexpected error occurred.';
                    notificationsService.error('Schema Export', 'Export failed: ' + msg);
                })
                .finally(function () {
                    $scope.loading = false;
                });
        };

        /**
         * Downloads the YAML file directly (re-exports server-side).
         */
        $scope.downloadYaml = function () {
            window.location.href = schemaExportResource.getDownloadYamlUrl();
        };

        /**
         * Downloads the ZIP file (YAML + media). Shows spinner on button.
         */
        $scope.downloadZip = function () {
            if ($scope.downloadingZip) { return; }

            $scope.downloadingZip = true;

            // Give Angular a tick to render the loading state before navigation
            setTimeout(function () {
                window.location.href = schemaExportResource.getDownloadZipUrl();

                // Re-enable the button after a short delay (download is async in browser)
                setTimeout(function () {
                    $scope.$apply(function () {
                        $scope.downloadingZip = false;
                    });
                }, 3000);
            }, 100);
        };

        // ─── Init ─────────────────────────────────────────────────────────────

        /**
         * On load, fetch statistics from the last export if available.
         */
        schemaExportResource.getStatistics()
            .then(function (response) {
                if (response.data && response.data.dataTypes !== undefined) {
                    applyStats(response.data);
                }
            })
            .catch(angular.noop); // silently ignore — no prior export is fine
    }
]);
