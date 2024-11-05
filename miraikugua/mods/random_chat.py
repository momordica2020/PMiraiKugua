from base_mod import BaseMod

class Mod1(BaseMod):
    def process(self, data):
        print(f"Mod1 processing data: {data}")
        # 插件的具体处理逻辑