# -*- coding: utf-8 -*-
"""
黄金钓鱼 - 美术资源批量生成脚本
使用混元大模型生成2D卡通风格图片，并自动抠除白色背景实现透明效果。

用法: python generate_all_art.py
"""

import os
import sys
import json
import time
import subprocess
import urllib.request
from PIL import Image, ImageDraw

# ===== 配置 =====
SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
TOKEN_FILE = os.path.normpath(os.path.join(SCRIPT_DIR, "..", "..", "..", ".token_temp.txt"))
BUDDY_SCRIPT = r"c:\Users\Administrator\.vscode\extensions\tencent-cloud.coding-copilot-4.9.29177644\out\extension\builtin\buddy-multimodal-generation\scripts\buddy-cloud.py"

# 资源根目录
ART_ROOT = SCRIPT_DIR

# 通用 prompt 后缀：要求纯白背景便于抠图
BG_WHITE = "pure white background, no shadow on background, flat lighting, centered"
BG_SCENE = "2d cartoon style, vibrant colors, clean lines, game art"

# ===== 资源定义 =====
# 每项: (子目录, 文件名, prompt, 是否抠图透明, 目标尺寸)
RESOURCES = [
    # ---- 背景 (不抠图) ----
    ("Backgrounds", "game_bg.png",
     f"2d cartoon game background, ocean scene, top portion is bright blue sky with white clouds, middle is blue sea surface with waves, bottom is deep underwater with bubbles, seabed with sand rocks and seaweed silhouettes, {BG_SCENE}", False, (1024, 1024)),
    ("Backgrounds", "underwater_bg.png",
     f"2d cartoon underwater background, deep blue gradient from light blue to dark navy, scattered translucent bubbles, {BG_SCENE}", False, (1024, 1024)),
    ("Backgrounds", "sky_bg.png",
     f"2d cartoon sky background, blue gradient sky with fluffy white clouds, {BG_SCENE}", False, (1024, 256)),
    ("Backgrounds", "water_surface.png",
     f"2d cartoon water surface texture, blue green wavy water surface seen from side, translucent waves, {BG_SCENE}", False, (1024, 128)),
    ("Backgrounds", "seabed.png",
     f"2d cartoon seabed, sandy ocean floor with rocks and green seaweed plants, {BG_SCENE}", False, (1024, 256)),

    # ---- 角色 (抠图) ----
    ("Characters", "fisherman.png",
     f"2d cartoon fisherman character, wearing straw hat and blue shirt, holding fishing rod, standing on a wooden boat, front view, {BG_WHITE}, {BG_SCENE}", True, (128, 128)),
    ("Characters", "fisherman_idle.png",
     f"2d cartoon fisherman character idle pose, wearing straw hat and blue shirt, relaxed standing, front view, {BG_WHITE}, {BG_SCENE}", True, (128, 128)),
    ("Characters", "boat.png",
     f"2d cartoon small wooden boat, brown wooden flat-bottom boat, side view, {BG_WHITE}, {BG_SCENE}", True, (128, 64)),

    # ---- 钩爪 (抠图) ----
    ("Hook", "hook.png",
     f"2d cartoon metal fishing hook, silver grey U-shaped hook with shiny highlight, {BG_WHITE}, {BG_SCENE}", True, (64, 64)),
    ("Hook", "hook_chain.png",
     f"2d cartoon metal chain link, silver grey vertical chain segment, {BG_WHITE}, {BG_SCENE}", True, (16, 64)),

    # ---- 物品: 鱼类 (抠图) ----
    ("Items", "small_fish.png",
     f"2d cartoon small yellow fish, orange body with yellow tail, big cute eye, side view, {BG_WHITE}, {BG_SCENE}", True, (128, 128)),
    ("Items", "medium_fish.png",
     f"2d cartoon medium fish, blue green body with stripes, side view, {BG_WHITE}, {BG_SCENE}", True, (128, 128)),
    ("Items", "big_fish.png",
     f"2d cartoon large fish, dark blue body, sharp mouth, side view, {BG_WHITE}, {BG_SCENE}", True, (128, 128)),
    ("Items", "shark.png",
     f"2d cartoon shark, grey blue body, sharp teeth visible, prominent dorsal fin, side view, {BG_WHITE}, {BG_SCENE}", True, (128, 128)),
    ("Items", "crab.png",
     f"2d cartoon red crab, round red shell with two big claws, front view, {BG_WHITE}, {BG_SCENE}", True, (128, 128)),
    ("Items", "jellyfish.png",
     f"2d cartoon jellyfish, translucent pink dome umbrella with hanging tentacles, front view, {BG_WHITE}, {BG_SCENE}", True, (128, 128)),

    # ---- 物品: 宝物 (抠图) ----
    ("Items", "pearl.png",
     f"2d cartoon white pearl, round white pearl with iridescent rainbow highlight, {BG_WHITE}, {BG_SCENE}", True, (128, 128)),
    ("Items", "treasure_chest.png",
     f"2d cartoon treasure chest, brown wooden chest with gold trim and gold lock, front view, {BG_WHITE}, {BG_SCENE}", True, (128, 128)),
    ("Items", "gold_nugget.png",
     f"2d cartoon gold nugget, golden yellow irregular chunk with shiny sparkles, {BG_WHITE}, {BG_SCENE}", True, (128, 128)),
    ("Items", "diamond.png",
     f"2d cartoon blue diamond gem, cyan blue faceted diamond with strong highlights, {BG_WHITE}, {BG_SCENE}", True, (128, 128)),
    ("Items", "mystery_box.png",
     f"2d cartoon mystery box, purple gift box with golden question mark on top, front view, {BG_WHITE}, {BG_SCENE}", True, (128, 128)),

    # ---- 物品: 道具 (抠图) ----
    ("Items", "bomb.png",
     f"2d cartoon bomb, black round bomb ball with lit fuse and sparks, {BG_WHITE}, {BG_SCENE}", True, (128, 128)),
    ("Items", "dynamite.png",
     f"2d cartoon dynamite bundle, red bound explosive sticks with fuse, {BG_WHITE}, {BG_SCENE}", True, (128, 128)),

    # ---- 物品: 垃圾 (抠图) ----
    ("Items", "trash.png",
     f"2d cartoon trash pile, grey brown crumpled trash ball, {BG_WHITE}, {BG_SCENE}", True, (128, 128)),
    ("Items", "boot.png",
     f"2d cartoon old boot, brown worn out boot, side view, {BG_WHITE}, {BG_SCENE}", True, (128, 128)),
    ("Items", "tin_can.png",
     f"2d cartoon tin can, silver cylindrical can with pull tab, side view, {BG_WHITE}, {BG_SCENE}", True, (128, 128)),
    ("Items", "seaweed.png",
     f"2d cartoon seaweed, green wavy kelp strips, {BG_WHITE}, {BG_SCENE}", True, (128, 128)),

    # ---- UI 按钮 (抠图) ----
    ("UI", "btn_normal.png",
     f"2d cartoon blue button, rounded rectangle blue button with highlight, no text, {BG_WHITE}, {BG_SCENE}", True, (256, 80)),
    ("UI", "btn_hover.png",
     f"2d cartoon bright blue button, rounded rectangle light blue button with highlight, no text, {BG_WHITE}, {BG_SCENE}", True, (256, 80)),
    ("UI", "btn_pressed.png",
     f"2d cartoon dark blue button, rounded rectangle dark blue button, no text, {BG_WHITE}, {BG_SCENE}", True, (256, 80)),
    ("UI", "btn_green.png",
     f"2d cartoon green button, rounded rectangle green button with highlight, no text, {BG_WHITE}, {BG_SCENE}", True, (256, 80)),
    ("UI", "btn_red.png",
     f"2d cartoon red button, rounded rectangle red button with highlight, no text, {BG_WHITE}, {BG_SCENE}", True, (256, 80)),

    # ---- UI 面板 (部分抠图) ----
    ("UI", "panel_bg.png",
     f"2d cartoon UI panel background, dark blue semi-transparent rounded rectangle panel with golden border, no text, {BG_WHITE}, {BG_SCENE}", True, (512, 512)),
    ("UI", "hud_bg.png",
     f"2d cartoon HUD bar background, dark semi-transparent horizontal strip bar, no text, {BG_WHITE}, {BG_SCENE}", True, (512, 64)),
    ("UI", "menu_bg.png",
     f"2d cartoon ocean panorama, beautiful ocean view with sky sea and underwater, game menu background, {BG_SCENE}", False, (1024, 1024)),

    # ---- UI 图标 (抠图) ----
    ("UI", "icon_coin.png",
     f"2d cartoon gold coin icon, golden circular coin with star symbol, {BG_WHITE}, {BG_SCENE}", True, (64, 64)),
    ("UI", "icon_clock.png",
     f"2d cartoon clock icon, round clock face with hands, {BG_WHITE}, {BG_SCENE}", True, (64, 64)),
    ("UI", "icon_target.png",
     f"2d cartoon target icon, bullseye dartboard with red white rings, {BG_WHITE}, {BG_SCENE}", True, (64, 64)),
    ("UI", "icon_star.png",
     f"2d cartoon gold star icon, shiny golden five-point star, {BG_WHITE}, {BG_SCENE}", True, (64, 64)),
    ("UI", "icon_fish.png",
     f"2d cartoon fish icon, simple cute fish silhouette, {BG_WHITE}, {BG_SCENE}", True, (64, 64)),
    ("UI", "icon_bomb.png",
     f"2d cartoon bomb icon, small black bomb with fuse, {BG_WHITE}, {BG_SCENE}", True, (64, 64)),

    # ---- Logo (不抠图，保留白底) ----
    ("Logo", "game_logo.png",
     f"2d cartoon game logo, title text reading GOLD FISHER with a fish hook and fish decoration, golden text on transparent, {BG_WHITE}, {BG_SCENE}", True, (512, 256)),
]


def load_token():
    """从临时文件读取token"""
    with open(TOKEN_FILE, "r") as f:
        return f.read().strip()


def generate_image(prompt, token, max_retries=6):
    """调用 buddy-cloud.py 生成图片，返回图片URL。遇到并发/限流错误自动重试。"""
    env = os.environ.copy()
    env["BUDDY_CLOUD_TOKEN"] = token
    for attempt in range(1, max_retries + 1):
        result = subprocess.run(
            ["python", BUDDY_SCRIPT, "image", prompt],
            capture_output=True, text=True, env=env, timeout=300
        )
        try:
            data = json.loads(result.stdout)
            if "result_url" in data and data["result_url"]:
                return data["result_url"][0]
            # 合并所有错误信息用于判断
            raw = data.get("raw_result", {})
            err_msg = str(raw.get("error", "")) + " " + str(data.get("error", "")) + " " + str(data.get("message", ""))
            err_lower = err_msg.lower()
            # 并发限制 / 任务上限 / 限流类错误 -> 重试
            if any(kw in err_lower for kw in ["concurrency", "concurrent", "任务上限", "limit", "quota", "please try", "请稍后重试", "rate"]):
                wait = 20 * attempt
                print(f"    [RETRY {attempt}/{max_retries}] 限流，等待{wait}秒后重试...")
                time.sleep(wait)
                continue
            print(f"    [ERROR] API返回: {str(data)[:200]}")
            return None
        except json.JSONDecodeError:
            combined = ((result.stdout or "") + (result.stderr or "")).lower()
            if any(kw in combined for kw in ["concurrency", "concurrent", "任务上限", "limit", "rate"]):
                wait = 20 * attempt
                print(f"    [RETRY {attempt}/{max_retries}] 限流，等待{wait}秒后重试...")
                time.sleep(wait)
                continue
            print(f"    [ERROR] JSON解析失败: {result.stdout[:300]}")
            return None
    print(f"    [ERROR] 达到最大重试次数({max_retries})")
    return None


def download_image(url, save_path):
    """下载图片"""
    try:
        req = urllib.request.Request(url, headers={"User-Agent": "Mozilla/5.0"})
        with urllib.request.urlopen(req, timeout=60) as resp:
            data = resp.read()
        with open(save_path, "wb") as f:
            f.write(data)
        return True
    except Exception as e:
        print(f"    [ERROR] 下载失败: {e}")
        return False


def remove_white_background(img, threshold=230):
    """
    抠图：将接近白色的像素转为透明。
    threshold: 亮度高于此值视为背景(0-255)
    使用边缘羽化使过渡更自然。
    """
    img = img.convert("RGBA")
    datas = img.getdata()
    new_data = []
    for item in datas:
        r, g, b, a = item
        # 计算亮度
        brightness = (r + g + b) / 3
        if brightness >= threshold:
            # 接近白色 -> 完全透明
            new_data.append((r, g, b, 0))
        elif brightness >= threshold - 40:
            # 过渡区域 -> 半透明
            alpha = int(a * (brightness - (threshold - 40)) / 40)
            alpha = max(0, min(255, 255 - alpha))
            new_data.append((r, g, b, alpha))
        else:
            new_data.append((r, g, b, a))
    img.putdata(new_data)
    return img


def trim_to_content(img, padding=4):
    """裁剪图片到非透明内容边界，加padding边距"""
    bbox = img.getbbox()
    if bbox:
        l, t, r, b = bbox
        l = max(0, l - padding)
        t = max(0, t - padding)
        r = min(img.width, r + padding)
        b = min(img.height, b + padding)
        img = img.crop((l, t, r, b))
    return img


def resize_image(img, target_size):
    """等比缩放图片到目标尺寸内（保持比例，不拉伸变形）"""
    tw, th = target_size
    w, h = img.size
    scale = min(tw / w, th / h)
    new_w = max(1, int(w * scale))
    new_h = max(1, int(h * scale))
    img = img.resize((new_w, new_h), Image.LANCZOS)

    # 创建目标尺寸画布，居中放置
    canvas = Image.new("RGBA", (tw, th), (0, 0, 0, 0))
    offset = ((tw - new_w) // 2, (th - new_h) // 2)
    canvas.paste(img, offset, img)
    return canvas


def process_image(src_path, dst_path, make_transparent, target_size):
    """处理图片：抠图、裁剪、缩放"""
    try:
        img = Image.open(src_path)
        img = img.convert("RGBA")

        if make_transparent:
            img = remove_white_background(img)
            img = trim_to_content(img, padding=4)

        img = resize_image(img, target_size)
        img.save(dst_path, "PNG")
        return True
    except Exception as e:
        print(f"    [ERROR] 图片处理失败: {e}")
        return False


def main():
    print("=" * 60)
    print("  黄金钓鱼 - 美术资源批量生成 (混元大模型)")
    print("=" * 60)

    # 加载token
    token = load_token()
    print(f"[INFO] Token加载成功")

    # 创建目录
    for subdir in set(r[0] for r in RESOURCES):
        dirpath = os.path.join(ART_ROOT, subdir)
        os.makedirs(dirpath, exist_ok=True)
    print(f"[INFO] 目录创建完成")

    # 临时下载目录
    tmp_dir = os.path.join(ART_ROOT, "_tmp_raw")
    os.makedirs(tmp_dir, exist_ok=True)

    total = len(RESOURCES)
    success = 0
    failed = 0
    failed_list = []

    for i, (subdir, filename, prompt, transparent, size) in enumerate(RESOURCES, 1):
        dst_path = os.path.join(ART_ROOT, subdir, filename)
        print(f"\n[{i}/{total}] 生成 {subdir}/{filename} ...")

        # 如果已存在则跳过（支持断点续传）
        if os.path.exists(dst_path):
            print(f"    [SKIP] 已存在，跳过")
            success += 1
            continue

        # 生成图片
        url = generate_image(prompt, token)
        if not url:
            failed += 1
            failed_list.append(f"{subdir}/{filename}")
            continue

        # 下载原始图片
        raw_path = os.path.join(tmp_dir, filename)
        if not download_image(url, raw_path):
            failed += 1
            failed_list.append(f"{subdir}/{filename}")
            continue

        # 处理图片（抠图+缩放）
        if process_image(raw_path, dst_path, transparent, size):
            print(f"    [OK] 完成 -> {subdir}/{filename} ({size[0]}x{size[1]})")
            success += 1
            # 删除临时文件
            try:
                os.remove(raw_path)
            except:
                pass
        else:
            failed += 1
            failed_list.append(f"{subdir}/{filename}")

        # 间隔避免限流
        time.sleep(3)

    # 清理临时目录
    try:
        if not os.listdir(tmp_dir):
            os.rmdir(tmp_dir)
    except:
        pass

    print("\n" + "=" * 60)
    print(f"  生成完成！成功: {success}/{total}, 失败: {failed}")
    if failed_list:
        print(f"  失败列表:")
        for f in failed_list:
            print(f"    - {f}")
        print(f"  重新运行脚本可自动重试失败项")
    print("=" * 60)


if __name__ == "__main__":
    main()
