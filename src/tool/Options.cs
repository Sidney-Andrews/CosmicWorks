using CommandLine;
using CommandLine.Text;
using System.Collections.Generic;

namespace CosmicWorks.Tool
{
    public class Options
    {
        [Option('r', "revision", Required = false, Default = Revision.v4, HelpText = "Revision of the database[s] to deploy")]
        public Revision Revision { get; set; }

        [Option('d', "datasets", Separator = ':', Required = false, Default = new Dataset[] { Dataset.customer, Dataset.product }, HelpText = "Selected database[s] to deploy")]
        public IEnumerable<Dataset> Datasets { get; set; }

        [Option('e', "emulator", Required = false, Default = false, HelpText = "Load data into emulator")]
        public bool Emulator { get; set; }

        [Option('p', "endpoint", Required = false, HelpText = "Set endpoint URL for target account")]
        public string Endpoint { get; set; }

        [Option('k', "key", Required = false, HelpText = "Set authorization key for target account")]
        public string Key { get; set; }

        [Usage(ApplicationAlias = "cosmicworks")]
        public static IEnumerable<Example> Examples
        {
            get
            {
                yield return new Example("Import all datasets using the latest version", new Options { });
                yield return new Example("Import all datasets into the emulator using the latest version", new Options { Emulator = true });
                yield return new Example("Import all datasets into an Azure Cosmos DB account using the latest version", new Options { Endpoint = "https://cosmicworks.documents.azure.com:443/", Key = "Djf5/R+o+4QDU5DE2qyMsEcaGQy67XIw/Jw==" });
                yield return new Example("Import all datasets using v4", new Options { Revision = Revision.v4 });
                yield return new Example("Import only the product dataset using v4", new Options { Revision = Revision.v4, Datasets = new Dataset[] { Dataset.product } });
                yield return new Example("Import only the customer and product datasets using v4", new Options { Revision = Revision.v4, Datasets = new Dataset[] { Dataset.customer, Dataset.product } });
            }
        }
    }
}
