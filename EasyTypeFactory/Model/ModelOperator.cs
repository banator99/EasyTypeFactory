namespace EasyTypeFactory.Model;

public abstract class ModelOperator;
internal sealed class ModelEqualityOperator : ModelOperator;
internal sealed class ModelInequalityOperator : ModelOperator;
