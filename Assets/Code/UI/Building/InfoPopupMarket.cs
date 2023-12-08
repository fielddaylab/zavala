using System;
using BeauRoutine;
using BeauUtil;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zavala.Economy;
using Zavala.Sim;

namespace Zavala.UI.Info {
    public class InfoPopupMarket : MonoBehaviour {
        public LayoutGroup Layout;
        public InfoPopupLocationRow[] Locations;
        public GameObject[] Dividers;
    }

    static public class InfoPopupMarketUtility {
        static public readonly Color PositiveColorBG = Colors.Hex("#C8E295");
        static public readonly Color PositiveColor = Colors.Hex("#078313");

        static public readonly Color NeutralColor = Colors.Hex("#806844");

        static public readonly Color NegativeColorBG = Colors.Hex("#FFBBBB");
        static public readonly Color NegativeColor = Colors.Hex("#C23636");

        static public void LoadLocationIntoRow(InfoPopupLocationRow row, OccupiesTile location, OccupiesTile referenceLocation) {
            LocationDescription desc = location.GetComponent<LocationDescription>();

            row.NameLabel.SetText(Loc.Find(desc.TitleLabel));
            row.Icon.sprite = desc.Icon;

            if (location.IsExternal) {
                row.RegionLabel.gameObject.SetActive(true);
                row.RegionLabel.SetText(Loc.Find("region.external.name"));
                row.NameLabel.rectTransform.SetAnchorPos(row.RegionLabel.rectTransform.sizeDelta.y / 2, Axis.Y);
            }
            else if (referenceLocation != null && location.RegionIndex == referenceLocation.RegionIndex) {
                row.RegionLabel.gameObject.SetActive(false);
                row.NameLabel.rectTransform.SetAnchorPos(0, Axis.Y);
            } else {
                row.RegionLabel.gameObject.SetActive(true);
                row.RegionLabel.SetText(Loc.Find(RegionUtility.GetNameLong(location.RegionIndex)));
                row.NameLabel.rectTransform.SetAnchorPos(row.RegionLabel.rectTransform.sizeDelta.y / 2, Axis.Y);
            }
        }

        static public void LoadCostsIntoRow(InfoPopupLocationRow row, MarketQueryResultInfo info, MarketConfig config, bool isSecondary) {
            row.PriceGroup.SetActive(true);

            int basePrice = config.DefaultPurchasePerRegion[info.Supplier.Position.RegionIndex].Buy[info.Resource];
            row.BasePriceRow.gameObject.SetActive(true);
            row.BasePriceRow.Number.SetText(basePrice.ToStringLookup());

            int shippingPrice = info.ShippingCost;
            row.ShippingRow.gameObject.SetActive(shippingPrice > 0);
            if (shippingPrice > 0) {
                row.ShippingRow.Number.SetText(shippingPrice.ToStringLookup());
            }

            int import = info.TaxRevenue.Import;
            row.ImportTaxRow.gameObject.SetActive(import > 0);
            row.SubsidyRow.gameObject.SetActive(import < 0);
            if (import > 0) {
                row.ImportTaxRow.Number.SetText((-import).ToStringLookup());
            } else if (import < 0) {
                row.SubsidyRow.Number.SetText((-import).ToStringLookup());
            }

            int salesTax = info.TaxRevenue.Sales;
            row.SalesTaxRow.gameObject.SetActive(salesTax > 0);
            if (salesTax > 0) {
                row.SalesTaxRow.Number.SetText((-salesTax).ToStringLookup());
            }

            int penalties = info.TaxRevenue.Penalties;
            row.PenaltyRow.gameObject.SetActive(penalties > 0);
            if (penalties > 0) {
                row.PenaltyRow.Number.SetText((-penalties).ToStringLookup());
            }

            int totalCost = basePrice + shippingPrice + import + salesTax + penalties;
            row.TotalProfitRow.gameObject.SetActive(false);
            row.TotalPriceRow.gameObject.SetActive(true);
            row.TotalPriceRow.Number.SetText(totalCost.ToStringLookup());

            if (isSecondary) {
                row.TotalPriceRow.Background.color = NegativeColorBG;
                row.TotalPriceRow.Number.color = NegativeColor;
            } else {
                row.TotalPriceRow.Background.color = PositiveColorBG;
                row.TotalPriceRow.Number.color = PositiveColor;
            }

            //row.PriceLayout.ForceRebuild(true);
        }

        static public void LoadProfitIntoRow(InfoPopupLocationRow row, MarketQueryResultInfo info, MarketConfig config, bool isSecondary) {
            row.PriceGroup.SetActive(true);

            int basePrice;
            if (info.Requester.IsLocalOption) {
                basePrice = 0;
            } else {
                basePrice = config.DefaultPurchasePerRegion[info.Supplier.Position.RegionIndex].Buy[info.Resource];
            }
            row.BasePriceRow.gameObject.SetActive(basePrice > 0);
            row.BasePriceRow.Number.SetText(basePrice.ToStringLookup());

            int shippingPrice = info.ShippingCost;
            row.ShippingRow.gameObject.SetActive(shippingPrice > 0);
            if (shippingPrice > 0) {
                row.ShippingRow.Number.SetText((-shippingPrice).ToStringLookup());
            }

            int import = info.TaxRevenue.Import;
            row.ImportTaxRow.gameObject.SetActive(import > 0);
            row.SubsidyRow.gameObject.SetActive(import < 0);
            if (import > 0) {
                row.ImportTaxRow.Number.SetText((-import).ToStringLookup());
            } else if (import < 0) {
                row.SubsidyRow.Number.SetText((-import).ToStringLookup());
            }

            // Commenting out for now - sales tax is applied to the buyer
            /*            
            int salesTax = info.TaxRevenue.Sales;
            row.SalesTaxRow.gameObject.SetActive(salesTax > 0);
            if (salesTax > 0) {
                row.SalesTaxRow.Number.SetText((-salesTax).ToStringLookup());
            }
            */

            int penalties = info.TaxRevenue.Penalties;
            row.PenaltyRow.gameObject.SetActive(penalties > 0);
            if (penalties > 0) {
                row.PenaltyRow.Number.SetText((-penalties).ToStringLookup());
            }

            int totalCost = info.Profit;
            row.TotalProfitRow.gameObject.SetActive(true);
            row.TotalPriceRow.gameObject.SetActive(false);
            row.TotalProfitRow.Number.SetText(totalCost.ToStringLookup());

            if (isSecondary) {
                row.TotalProfitRow.Background.color = NegativeColorBG;
                row.TotalProfitRow.Number.color = NegativeColor;
            } else {
                row.TotalProfitRow.Background.color = PositiveColorBG;
                row.TotalProfitRow.Number.color = PositiveColor;
            }

            //row.PriceLayout.ForceRebuild(true);
        }

        static public void ClearPricesAtRow(InfoPopupLocationRow row) {
            row.PriceGroup.SetActive(false);
        }
    }
}