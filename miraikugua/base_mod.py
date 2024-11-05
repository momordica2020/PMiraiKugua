class BaseMod:
    def process(self, data):
        """处理数据的接口，所有插件都需要实现此方法"""
        raise NotImplementedError("Subclasses must implement this method")