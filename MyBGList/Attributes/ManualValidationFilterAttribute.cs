using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using System.ComponentModel.DataAnnotations;

namespace MyBGList.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class ManualValidationFilterAttribute : Attribute, IActionModelConvention
    {
        public void Apply(ActionModel action)
        {
            for (int i = 0; i < action.Filters.Count; i++)
            {
                if (action.Filters[i] is ModelStateInvalidFilter
                    || action.Filters[i].GetType().Name == "ModelStateInvalidFilterFactory") // ugly, but it is internal... 
                {
                    action.Filters.RemoveAt(i);
                    break;
                }
            }
        }
    }
}
