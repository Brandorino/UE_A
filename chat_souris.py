import pygame
import random
import heapq
import matplotlib.pyplot as plt
import time
import math

pygame.init()

CELL_SIZE = 30
GRID_WIDTH = 25
GRID_HEIGHT = 25
SPEED_MULTIPLIER_CHAT = 0.8  
MOUSE_SPEED = 1  
MOUSE_DUPLICATION_THRESHOLD = 3
INITIAL_MOUSE_COUNT = 3  
INITIAL_CHAT_COUNT = 2  
EATING_DISTANCE = 1.0  

BLACK = (0, 0, 0)
GRAY = (169, 169, 169)
YELLOW = (255, 255, 0)
WHITE = (255, 255, 255)

try:
    with open('map.txt', 'r') as f:
        game_map = [line.strip() for line in f.readlines()]
except FileNotFoundError:
    print("Erreur : fichier 'map.txt' introuvable.")
    pygame.quit()
    exit()

GRID_HEIGHT = len(game_map)
GRID_WIDTH = len(game_map[0]) if GRID_HEIGHT > 0 else 0

screen = pygame.display.set_mode((GRID_WIDTH * CELL_SIZE, GRID_HEIGHT * CELL_SIZE))
pygame.display.set_caption('Chat et Souris')

mouse_population_over_time = []
start_time = time.time()

mouse_list = []
chat_list = []
cheese_list = []

class Entity:
    def __init__(self, x, y):
        self.x = x
        self.y = y

    def draw(self, color, shape='circle'):
        if shape == 'circle':
            pygame.draw.circle(screen, color, (self.x * CELL_SIZE + CELL_SIZE // 2, self.y * CELL_SIZE + CELL_SIZE // 2), CELL_SIZE // 3)
        elif shape == 'triangle':
            pygame.draw.polygon(screen, color, [(self.x * CELL_SIZE + CELL_SIZE // 2, self.y * CELL_SIZE),
                                                (self.x * CELL_SIZE, self.y * CELL_SIZE + CELL_SIZE),
                                                (self.x * CELL_SIZE + CELL_SIZE, self.y * CELL_SIZE + CELL_SIZE)])

    def distance_to(self, other):
        return math.sqrt((self.x - other.x) ** 2 + (self.y - other.y) ** 2)

class Mouse(Entity):
    def __init__(self, x, y):
        super().__init__(x, y)
        self.cheese_collected = 0
        self.target_cheese = None

    def move_towards(self, target):
        path = a_star((self.x, self.y), target, for_mouse=True)
        if path:
            next_pos = path[min(MOUSE_SPEED, len(path) - 1)]
            if not is_entity_at(next_pos, exclude_self=self):
                self.x, self.y = next_pos

    def collect_cheese(self):
        if self.target_cheese and self.distance_to(self.target_cheese) <= EATING_DISTANCE:
            self.cheese_collected += 1
            cheese_list.remove(self.target_cheese)
            self.target_cheese = None
            cheese_list.append(spawn_cheese())
            if self.cheese_collected >= MOUSE_DUPLICATION_THRESHOLD:
                mouse_list.append(Mouse(self.x, self.y))

    def flee_from_chats(self):
        max_distance = -1
        best_pos = (self.x, self.y)
        for neighbor in get_neighbors((self.x, self.y), for_mouse=True):
            distance = sum(heuristic(neighbor, (chat.x, chat.y)) for chat in chat_list)
            if distance > max_distance and not is_entity_at(neighbor):
                max_distance = distance
                best_pos = neighbor
        self.x, self.y = best_pos

class Chat(Entity):
    def __init__(self, x, y):
        super().__init__(x, y)
        self.target_mouse = None

    def move_towards(self, target):
        path = a_star((self.x, self.y), target, for_mouse=False)
        if path:
            next_pos = path[min(max(1, int(MOUSE_SPEED * SPEED_MULTIPLIER_CHAT)), len(path) - 1)]
            if not is_entity_at(next_pos, exclude_self=self):
                self.x, self.y = next_pos

    def catch_mouse(self):
        if self.target_mouse and self.distance_to(self.target_mouse) <= EATING_DISTANCE:
            mouse_list.remove(self.target_mouse)
            self.target_mouse = None

# Fonctions d'affichage
def draw_grid():
    for y, row in enumerate(game_map):
        for x, cell in enumerate(row):
            if cell == 'x':
                pygame.draw.rect(screen, BLACK, (x * CELL_SIZE, y * CELL_SIZE, CELL_SIZE, CELL_SIZE))
            elif cell == 'o':
                pygame.draw.rect(screen, GRAY, (x * CELL_SIZE, y * CELL_SIZE, CELL_SIZE, CELL_SIZE))

def a_star(start, goal, for_mouse):
    open_set = []
    heapq.heappush(open_set, (0, start))
    came_from = {}
    g_score = {start: 0}
    f_score = {start: heuristic(start, goal)}

    while open_set:
        _, current = heapq.heappop(open_set)

        if current == goal:
            return reconstruct_path(came_from, current)

        for neighbor in get_neighbors(current, for_mouse):
            tentative_g_score = g_score[current] + 1

            if neighbor not in g_score or tentative_g_score < g_score[neighbor]:
                came_from[neighbor] = current
                g_score[neighbor] = tentative_g_score
                f_score[neighbor] = tentative_g_score + heuristic(neighbor, goal)
                heapq.heappush(open_set, (f_score[neighbor], neighbor))

    return []

def heuristic(a, b):
    return abs(a[0] - b[0]) + abs(a[1] - b[1])

def get_neighbors(pos, for_mouse):
    x, y = pos
    neighbors = [(x + dx, y + dy) for dx, dy in [(-1, 0), (1, 0), (0, -1), (0, 1)]]
    valid_neighbors = [n for n in neighbors if is_walkable(n, for_mouse)]
    return valid_neighbors

def is_walkable(pos, for_mouse):
    x, y = pos
    if 0 <= x < GRID_WIDTH and 0 <= y < GRID_HEIGHT:
        cell = game_map[y][x]
        if cell == 'x':
            return False
        if cell == 'o' and not for_mouse:
            return False
        return True
    return False

def is_entity_at(pos, exclude_self=None):
    x, y = pos
    for entity in mouse_list + chat_list + cheese_list:
        if entity.x == x and entity.y == y and entity != exclude_self:
            return True
    return False

def reconstruct_path(came_from, current):
    path = [current]
    while current in came_from:
        current = came_from[current]
        path.append(current)
    path.reverse()
    return path

def spawn_cheese():
    while True:
        x, y = random.randint(0, GRID_WIDTH - 1), random.randint(0, GRID_HEIGHT - 1)
        if game_map[y][x] != 'x' and not is_entity_at((x, y)):
            return Entity(x, y)

def assign_targets():
    for chat in chat_list:
        if not chat.target_mouse or chat.target_mouse not in mouse_list:
            available_mice = [mouse for mouse in mouse_list if mouse not in [c.target_mouse for c in chat_list]]
            if available_mice:
                chat.target_mouse = random.choice(available_mice)

    for mouse in mouse_list:
        if not mouse.target_cheese or mouse.target_cheese not in cheese_list:
            available_cheeses = [cheese for cheese in cheese_list if cheese not in [m.target_cheese for m in mouse_list]]
            if available_cheeses:
                mouse.target_cheese = random.choice(available_cheeses)

mouse_list = [Mouse(random.randint(0, GRID_WIDTH - 1), random.randint(0, GRID_HEIGHT - 1)) for _ in range(INITIAL_MOUSE_COUNT)]
chat_list = [Chat(random.randint(0, GRID_WIDTH - 1), random.randint(0, GRID_HEIGHT - 1)) for _ in range(INITIAL_CHAT_COUNT)]
cheese_list = [spawn_cheese() for _ in range(max(1, INITIAL_MOUSE_COUNT // 2))]

running = True
clock = pygame.time.Clock()

while running:
    screen.fill(WHITE)
    draw_grid()

    for event in pygame.event.get():
        if event.type == pygame.QUIT:
            running = False

    if time.time() - start_time >= 1:
        mouse_population_over_time.append(len(mouse_list))
        start_time = time.time()

    assign_targets()

    for mouse in mouse_list:
        if mouse.target_cheese:
            mouse.move_towards((mouse.target_cheese.x, mouse.target_cheese.y))
            mouse.collect_cheese()
        else:
            mouse.flee_from_chats()
        mouse.draw(GRAY)

    for chat in chat_list:
        if chat.target_mouse:
            chat.move_towards((chat.target_mouse.x, chat.target_mouse.y))
            chat.catch_mouse()
        chat.draw(BLACK)

    for cheese in cheese_list:
        cheese.draw(YELLOW, 'triangle')

    pygame.display.flip()
    clock.tick(5)

pygame.quit()

plt.plot(mouse_population_over_time)
plt.xlabel('Temps (secondes)')
plt.ylabel('Population des souris')
plt.title('Evolution de la population des souris au cours du temps')
plt.show()
