using BDFramework.RuntimeTests.Contracts;
using Talos.E2E;

namespace BDFramework.Test.E2E
{
    /// <summary>
    /// CSV 工具可打包契约测试套件。
    /// Packaged CSV-tool contract test suite.
    /// 该套件覆盖 CSV 纯逻辑加载与保存往返，保证开发期配置表工具在真机和 BatchMode 下都能稳定回归。
    /// This suite covers pure-logic CSV loading and save round-trips, ensuring the development-time configuration table tooling can regress safely in player builds and BatchMode.
    /// </summary>
    public static class CsvContractTests
    {
        [E2ETest(suite: "csv-contract", order: 1, des: "csv-load-objects")]
        public static void CsvLoadObjects()
        {
            CsvContractAssertions.VerifyLoadObjectsParsesHeaderAndValues();
        }

        [E2ETest(suite: "csv-contract", order: 2, des: "csv-quoted-and-ignored-columns")]
        public static void CsvQuotedAndIgnoredColumns()
        {
            CsvContractAssertions.VerifyLoadObjectsSupportsQuotedStringsAndIgnoredColumns();
        }

        [E2ETest(suite: "csv-contract", order: 3, des: "csv-save-load-roundtrip")]
        public static void CsvSaveLoadRoundTrip()
        {
            CsvContractAssertions.VerifySaveObjectsAndLoadObjectsRoundTrip();
        }

        [E2ETest(suite: "csv-contract", order: 4, des: "csv-load-single-object")]
        public static void CsvLoadSingleObject()
        {
            CsvContractAssertions.VerifyLoadObjectMapsSingleObjectFields();
        }
    }
}