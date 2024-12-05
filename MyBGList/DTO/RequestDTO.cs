﻿using MyBGList.Attributes;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace MyBGList.DTO
{
    public class RequestDTO
    {
        [DefaultValue(0)] // For SwaggerGen to generate proper defaults in swagger.json
        public int PageIndex { get; set; } = 0;

        [DefaultValue(10)]
        [Range(1, 100)]
        public int PageSize { get; set; } = 10;

        [DefaultValue("Name")]
        [SortColumnValidator(typeof(BoardGameDTO))]
        public string? SortColumn { get; set; } = "Name";

        [DefaultValue("ASC")]
        [SortOrderValidator]
        public string? SortOrder { get; set; } = "ASC";

        [DefaultValue(null)]
        public string? Filter { get; set; } = null;
    }
}