﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CsvHelper;
using VirtoCommerce.CatalogCsvImportModule.Data.Model;
using VirtoCommerce.CatalogCsvImportModule.Data.Services;
using VirtoCommerce.Domain.Catalog.Model;
using Xunit;

namespace VirtoCommerce.CatalogCsvImportModule.Test
{
    public class MappingTests
    {
        [Fact]
        public void CsvProductMapTest_CsvHasPropertyValues_PropertyValuesMapped()
        {
            var importInfo = new CsvImportInfo { Configuration = CsvProductMappingConfiguration.GetDefaultConfiguration() };
            importInfo.Configuration.CsvColumns = new[] { "Sku" };
            importInfo.Configuration.PropertyCsvColumns = new[] { "ProductProperty", "ProductProperty_Multivalue" };
            importInfo.Configuration.Delimiter = ",";

            string path = @"../../data/product-propertyvalues.csv";
            var csvProducts = ReadCsvFile(path, importInfo);

            Action<PropertyValue>[] inspectorsFirstProduct = {
                x => Assert.True((string) x.Value == "Product-1-propertyvalue-test" && x.PropertyName =="ProductProperty"),
                x => Assert.True((string) x.Value == "Product-1-multivalue-1, Product-1-multivalue-2" && x.PropertyName =="ProductProperty_Multivalue")
            };
            Action<PropertyValue>[] inspectorsSecond = {
                x => Assert.True((string) x.Value == "Product-2-propertyvalue-test" && x.PropertyName =="ProductProperty"),
                x => Assert.True((string) x.Value == "Product-2-multivalue-1, Product-2-multivalue-1, Product-2-multivalue-3" && x.PropertyName =="ProductProperty_Multivalue")
            };
            Assert.Collection(csvProducts.FirstOrDefault().PropertyValues, inspectorsFirstProduct);
            Assert.Collection(csvProducts.LastOrDefault().PropertyValues, inspectorsSecond);
        }

        [Fact]
        public void CsvProductMapTest_CsvHasProductProperties_PropertiesMapped()
        {
            var importInfo = new CsvImportInfo { Configuration = CsvProductMappingConfiguration.GetDefaultConfiguration() };
            importInfo.Configuration.Delimiter = ",";

            string path = @"../../data/product-productproperties.csv";
            var csvProducts = ReadCsvFile(path, importInfo);
            var product = csvProducts.FirstOrDefault();

            Assert.Equal("429408", product.Id);
            Assert.Equal("CBLK21113", product.Sku);
            Assert.Equal("cblk21113-product-1", product.Name);
            Assert.Equal("catId_1", product.CategoryId);
            Assert.Equal("GTIN_Value", product.Gtin);
            Assert.Equal("mainprod_123", product.MainProductId);
            Assert.Equal("Vendor_value", product.Vendor);
            Assert.Equal("ProductType_value", product.ProductType);
            Assert.Equal("ShippingType_value", product.ShippingType);
            Assert.Equal("DownloadType_value", product.DownloadType);
            Assert.True(product.HasUserAgreement);
            Assert.True(product.IsBuyable);
            Assert.True(product.TrackInventory);
        }

        [Fact]
        public void CsvProductMapTest_CsvHasPriceAndQuantity_PriceAndQuantityMapped()
        {
            var importInfo = new CsvImportInfo { Configuration = CsvProductMappingConfiguration.GetDefaultConfiguration() };
            importInfo.Configuration.Delimiter = ",";

            string path = @"../../data/product-productproperties-priceQuantity.csv";
            var csvProducts = ReadCsvFile(path, importInfo);
            var product = csvProducts.FirstOrDefault();

            Assert.Equal("123.4", product.ListPrice);
            Assert.Equal(123.4m, product.Price.List);
            Assert.Equal("456.7", product.SalePrice);
            Assert.Equal(456.7m, product.Price.Sale);
            Assert.Equal("EUR", product.Currency);
            Assert.Equal("EUR", product.Price.Currency);
            Assert.Equal("5", product.Quantity);
            Assert.Equal(5, product.Inventory.InStockQuantity);
        }

        [Fact]
        public void CsvProductMapTest_CsvHasSeoInfo_SeoInfoMapped()
        {
            var importInfo = new CsvImportInfo { Configuration = CsvProductMappingConfiguration.GetDefaultConfiguration() };
            importInfo.Configuration.Delimiter = ",";

            string path = @"../../data/product-productproperties-seoInfo.csv";
            var csvProducts = ReadCsvFile(path, importInfo);
            var product = csvProducts.FirstOrDefault();

            Assert.Equal("seo-slug-url", product.SeoUrl);
            Assert.Equal("seo-slug-url", product.SeoInfo.SemanticUrl);
            Assert.Equal("Seo_Title_Value", product.SeoTitle);
            Assert.Equal("Seo_Title_Value", product.SeoInfo.PageTitle);
            Assert.Equal("Seo_Descr_Value", product.SeoDescription);
            Assert.Equal("Seo_Descr_Value", product.SeoInfo.MetaDescription);
        }

        [Fact]
        public void CsvProductMapTest_CsvHasReview_ReviewMapped()
        {
            var importInfo = new CsvImportInfo { Configuration = CsvProductMappingConfiguration.GetDefaultConfiguration() };
            importInfo.Configuration.Delimiter = ",";

            string path = @"../../data/product-productproperties-review.csv";
            var csvProducts = ReadCsvFile(path, importInfo);
            var product = csvProducts.FirstOrDefault();

            Assert.Equal("Review_Content", product.Review);
            Assert.Equal("Review_Content", product.EditorialReview.Content);
            Assert.Equal("ReviewType_Value", product.ReviewType);
            Assert.Equal("ReviewType_Value", product.EditorialReview.ReviewType);
        }

        [Fact]
        public void CsvProductMapTest_CsvHasCategorypath_CategoryPathMapped()
        {
            var importInfo = new CsvImportInfo { Configuration = CsvProductMappingConfiguration.GetDefaultConfiguration() };
            importInfo.Configuration.Delimiter = ",";

            string path = @"../../data/product-productproperties-categoryPath.csv";
            var csvProducts = ReadCsvFile(path, importInfo);
            var product = csvProducts.FirstOrDefault();

            Assert.Equal("TestCategory1", product.CategoryPath);
            Assert.Equal("TestCategory1", product.Category.Path);
        }

        [Fact]
        public void CsvProductMapTest_MappingHasDefaultCategoryPath_DefaultCategoryPathMapped()
        {
            var defaultValue = "Custom_category_path_value";

            var importInfo = new CsvImportInfo { Configuration = CsvProductMappingConfiguration.GetDefaultConfiguration() };
            importInfo.Configuration.Delimiter = ",";

            var categoryPathMapping = importInfo.Configuration.PropertyMaps.FirstOrDefault(x => x.EntityColumnName == "CategoryPath");
            categoryPathMapping.CsvColumnName = null;
            categoryPathMapping.CustomValue = defaultValue;

            string path = @"../../data/product-productproperties-noCategoryPath.csv";
            var csvProducts = ReadCsvFile(path, importInfo);
            var product = csvProducts.FirstOrDefault();

            Assert.Equal(defaultValue, product.CategoryPath);
            Assert.Equal(defaultValue, product.Category.Path);
        }

        private List<CsvProduct> ReadCsvFile(string path, CsvImportInfo importInfo)
        {
            var csvProducts = new List<CsvProduct>();
            using (FileStream fs = File.Open(path, FileMode.Open))
            {
                using (var reader = new CsvReader(new StreamReader(fs)))
                {
                    reader.Configuration.Delimiter = importInfo.Configuration.Delimiter;
                    reader.Configuration.RegisterClassMap(new CsvProductMap(importInfo.Configuration));
                    reader.Configuration.WillThrowOnMissingField = false;
                    reader.Configuration.TrimFields = true;

                    while (reader.Read())
                    {
                        var csvProduct = reader.GetRecord<CsvProduct>();
                        csvProducts.Add(csvProduct);
                    }
                }
            }
            return csvProducts;
        }
    }
}
