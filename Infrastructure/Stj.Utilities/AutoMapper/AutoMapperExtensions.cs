using AutoMapper;
using System;
using System.Linq;

namespace Stj.Utilities.AutoMapper
{
    public static class AutoMapperMapperExtensions
    {
        public static IMappingExpression<TSource, TDestination> InheritMappingFromBaseType<TSource, TDestination>(this IMappingExpression<TSource, TDestination> mappingExpression)
        {
            var sourceType = typeof(TSource);
            var desctinationType = typeof(TDestination);
            var sourceParentType = sourceType.BaseType;
            var destinationParentType = desctinationType.BaseType;

            mappingExpression
                .BeforeMap((x, y) => Mapper.Map(x, y, sourceParentType, destinationParentType))
                .ForAllMembers(x => x.Condition(r => NotAlreadyMapped(sourceParentType, destinationParentType, r)));
            return mappingExpression;
        }

        public static IMappingExpression<TSource, TDestination> InheritMappingFromDestinationBaseType<TSource, TDestination>(this IMappingExpression<TSource, TDestination> mappingExpression)
        {
            var sourceType = typeof(TSource);
            var desctinationType = typeof(TDestination);
            var destinationParentType = desctinationType.BaseType;

            mappingExpression
                .BeforeMap((x, y) => Mapper.Map(x, y, sourceType, destinationParentType))
                .ForAllMembers(x => x.Condition(r => NotAlreadyMapped(sourceType, destinationParentType, r)));

            return mappingExpression;
        }

        private static bool NotAlreadyMapped(Type sourceType, Type desitnationType, ResolutionContext r)
        {
            return !r.IsSourceValueNull &&
                   Mapper.FindTypeMapFor(sourceType, desitnationType).GetPropertyMaps().Where(
                       m => m.DestinationProperty.Name.Equals(r.MemberName)).Select(y => !y.IsMapped()).All(b => b);
        }
    }
}
