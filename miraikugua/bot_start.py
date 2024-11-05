from graia.ariadne.app import Ariadne
from graia.ariadne.entry import config
from graia.ariadne.message.chain import MessageChain
from graia.ariadne.message.element import Plain
from graia.ariadne.model import Friend
import importlib
import os
import pkgutil
from base_mod import BaseMod

class ModLoader:
    def __init__(self, mods_folder="mods"):
        self.mods = []
        self.load_mods(mods_folder)

    def load_mods(self, mods_folder):
        """加载所有插件"""
        package = mods_folder
        for _, mod_name, _ in pkgutil.iter_modules([package]):
            mod = importlib.import_module(f"{package}.{mod_name}")
            for attr_name in dir(mod):
                attr = getattr(mod, attr_name)
                # 检查是否为 BaseMod 的子类并实例化
                if isinstance(attr, type) and issubclass(attr, BaseMod) and attr is not BaseMod:
                    print(f"Loading mod: {mod_name}")
                    self.mods.append(attr())  # 实例化插件类

    def process_data(self, data):
        """依次调用每个插件的 process 方法"""
        for mod in self.mods:
            mod.process(data)

# 主函数
if __name__ == "__main__":
    
    loader = ModLoader()
    data = "Some data to process"
    loader.process_data(data)
    app = Ariadne(
        config(
            verify_key="",  # 填入 VerifyKey
            account=2963959417,  # 你的机器人的 qq 号
        ),
    )

    @app.broadcast.receiver("FriendMessage")
    async def friend_message_listener(app: Ariadne, friend: Friend):
        await app.send_message(friend, MessageChain([Plain("Hello, World!")]))
        # 实际上 MessageChain(...) 有没有 "[]" 都没关系
    # @app.broadcast.receiver("GroupMessage")
    # async def friend_message_listener(app: Ariadne, friend: Friend):
    #     await app.send_message(friend, MessageChain([Plain("Hello, World!")]))

    app.launch_blocking()