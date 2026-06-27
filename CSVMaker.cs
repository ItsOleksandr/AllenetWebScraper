using System.Globalization;
using System.Text;

namespace AllegroParse;

public static class CSVMaker
{
    public static string GetCSV(List<ProductInfo> products)
    {
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.AppendLine("EAN;Liczba;Cena");

        foreach (var product in products)
        {
            stringBuilder.AppendLine(string.Join(";",product.EAN ,product.Count , (product.Price * 3).ToString(CultureInfo.InvariantCulture)));
        }
        return stringBuilder.ToString();
    }
}