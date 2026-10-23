using Google.Protobuf.Collections;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Request.Domain;
using Riok.Mapperly.Abstractions;
using Tetris.Protocol.Common;
using static Pharos.Server.Request.Web.Controllers.RequestsController;
using InstrumentTestOrder = Tetris.Protocol.Instrument.TestOrder.TestOrder;
using ProtocolTestOrder = Tetris.Protocol.Instrument.TestOrder;

namespace Pharos.Server.Request.Application.Mapping;

[Mapper]
internal static partial class TestOrderMapper
{
    public static async Task<RepeatedField<InstrumentTestOrder>> MapTestOrders(IEnumerable<TestOrder> testOrders, IArticlesService articles)
    {
        var testOrdersToReturn = new RepeatedField<InstrumentTestOrder>();

        foreach (var sampleTestOrder in testOrders)
        {
            var testOrder = ToInstrumentTestOrder(sampleTestOrder);

            var custom = sampleTestOrder.TestOrderCustomParameters;
            if (custom != null)
            {
                if (!string.IsNullOrEmpty(custom.CarrierLot) && custom.CarrierLot.Length >= 3)
                {
                    var article = await articles.GetArticle(custom.CarrierLot[..3]);
                    var articleType = EnumReagentType.ImmunoCapCarrier;
                    if (article != null)
                    {
                        if (article.ArticleType == ArticleType.EliaCalibratorWellCarrier)
                        {
                            articleType = EnumReagentType.EliaCalibrationWellCarrier;
                        }
                        else if (article.ArticleType == ArticleType.EliaWellCarrier)
                        {
                            articleType = EnumReagentType.EliaWellCarrier;
                        }
                    }

                    AddLotNumber(testOrder, custom.CarrierLot, articleType);
                }

                AddLotNumber(testOrder, custom.ConjugateLot, EnumReagentType.ConjugateCartridge);
                AddLotNumber(testOrder, custom.DevelopmentLot, EnumReagentType.DevelopmentSolutionCartridge);
                AddLotNumber(testOrder, custom.DiluentLot, EnumReagentType.DiluentCartridge);
            }

            testOrdersToReturn.Add(testOrder);
        }

        return testOrdersToReturn;
    }

    [MapperIgnoreTarget(nameof(InstrumentTestOrder.CmrTestId))]
    [MapperIgnoreTarget(nameof(InstrumentTestOrder.LotNumbers))]
    public static partial InstrumentTestOrder ToInstrumentTestOrder(VolumeCalculationTestOrder source);

    [MapProperty(nameof(TestOrder.Uid), nameof(InstrumentTestOrder.TestOrderId))]
    [MapProperty(nameof(TestOrder.TestSortOrder), nameof(InstrumentTestOrder.ExecutionOrder))]
    [MapperIgnoreTarget(nameof(InstrumentTestOrder.LotNumbers))]
    [MapProperty(nameof(TestOrder.TestOrderCustomParameters.CustomTestId), nameof(InstrumentTestOrder.CmrTestId), Use = nameof(MapCmrTestId))]
    private static partial InstrumentTestOrder ToInstrumentTestOrder(TestOrder source);

    private static string MapCmrTestId(string? source)
    {
        return source ?? string.Empty;
    }

    private static void AddLotNumber(InstrumentTestOrder testOrder, string? lotNumber, EnumReagentType reagentType)
    {
        if (!string.IsNullOrEmpty(lotNumber))
        {
            testOrder.LotNumbers.Add(new ProtocolTestOrder.LotNumber
            {
                LotNumber_ = lotNumber,
                ReagentType = reagentType
            });
        }
    }
}
