using System.Linq.Expressions;

namespace Kirel.Shared;
/// <summary>
/// Class which constructs LINQ predicates piece by piece
/// </summary>
public class PredicateBuilder
{
    /// <summary>
    /// Method for creating an Expression that initially evaluates true
    /// </summary>
    /// <typeparam name="T">Class type</typeparam>
    /// <returns>Expression</returns>
    public static Expression<Func<T, bool>> True<T>()
    {
        return _ => true;
    }
    /// <summary>
    /// Method for creating an Expression that initially evaluates true
    /// </summary>
    /// <typeparam name="T">Class type</typeparam>
    /// <returns>Expression</returns>
    public static Expression<Func<T, bool>> False<T>()
    {
        return _ => false;
    }
    /// <summary>
    /// Class implements a keyword search
    /// </summary>
    /// <param name="keyword">Searching keyword</param>
    /// <param name="virtualProperties">Search in virtual properties flag</param>
    /// <typeparam name="T">Class type</typeparam>
    /// <returns>Expression</returns>
    public static Expression<Func<T, bool>> PredicateSearchInAllFields<T>(string keyword, bool virtualProperties = false)
    {
        if (string.IsNullOrEmpty(keyword))
            return True<T>();
        var predicate = False<T>();
        var properties = typeof(T).GetProperties().AsEnumerable();
        if (!virtualProperties)
            properties = properties.Where(p => p.GetGetMethod()?.IsVirtual is false);
        foreach (var propertyInfo in properties)
        {
            var parameter = Expression.Parameter(typeof(T), "x");
            var property = Expression.Property(parameter, propertyInfo);
            var propertyAsObject = Expression.Convert(property, typeof(object));
            var nullCheck = Expression.NotEqual(propertyAsObject, Expression.Constant(null, typeof(object)));
            var propertyAsString = Expression.Call(property, "ToString", null, null);
            var keywordExpression = Expression.Constant(keyword);
            var contains = propertyInfo.PropertyType == typeof(string) ? 
                Expression.Call(property, "Contains", null, keywordExpression) : 
                Expression.Call(propertyAsString, "Contains", null, keywordExpression);
            var lambda = Expression.Lambda(Expression.AndAlso(nullCheck, contains), parameter);
            predicate = Or(predicate, (Expression<Func<T, bool>>)lambda);
        }

        return predicate;
    }
    /// <summary>
    /// Wrapping in a new lambda expression
    /// </summary>
    /// <param name="propertyName">Property name</param>
    /// <typeparam name="T">Class type</typeparam>
    /// <returns>Expression</returns>
    public static Expression<Func<T, object>>? ToLambda<T>(string propertyName)
    {
        // if (typeof(T)
        //     .GetProperties().Any(p => p.GetGetMethod()?.IsVirtual is false &&
        //                               p.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase)))
        //     return null;
        
        var parameter = Expression.Parameter(typeof(T));
        var property = Expression.Property(parameter, propertyName);
        var propAsObject = Expression.Convert(property, typeof(object));

        return Expression.Lambda<Func<T, object>>(propAsObject, parameter);
    }
    /// <summary>
    /// Or conditions method
    /// </summary>
    /// <param name="expr1">First expression</param>
    /// <param name="expr2">Second expression</param>
    /// <typeparam name="T">Class type</typeparam>
    /// <returns>Expression</returns>
    public static Expression<Func<T, bool>> Or<T>(Expression<Func<T, bool>> expr1, Expression<Func<T, bool>> expr2)
    {
        var invokedExpr = Expression.Invoke(expr2, expr1.Parameters.Cast<Expression>());
        return Expression.Lambda<Func<T, bool>>(Expression.OrElse(expr1.Body, invokedExpr), expr1.Parameters);
    }
    /// <summary>
    /// And conditions method
    /// </summary>
    /// <param name="expr1">First expression</param>
    /// <param name="expr2">Second expression</param>
    /// <typeparam name="T">Class type</typeparam>
    /// <returns>Expression</returns>
    public static Expression<Func<T, bool>> And<T>(Expression<Func<T, bool>> expr1, Expression<Func<T, bool>> expr2)
    {
        var invokedExpr = Expression.Invoke(expr2, expr1.Parameters.Cast<Expression>());
        return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(expr1.Body, invokedExpr), expr1.Parameters);
    }
}