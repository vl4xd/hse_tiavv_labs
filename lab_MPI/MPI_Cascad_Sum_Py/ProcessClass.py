class Node():
    


if __name__ == "__main__":
    memory = [self.root]    # память (стек)
                                    # в начале память содержит ссылку на корень заданного дерева
            is_first_leaf = True # флаг для записи первого листа с минимальным f_value
            # внешний цикл, перебирающий линии заглублейний
            # закончить цикл, если не получается извлечь ссылку из памяти (стека)
            while memory:
                cur_ref = memory.pop() # текущая ссылка
                # внутренний цикл обхода каждой линии заглубления дерева до листа
                while True:
                    # обработка данных узла...
                    # если находим необработанное завершение игры
                    if cur_ref == self.goal:
                        self.__is_game_finished = True # переопределяем флаг завершения

                    if cur_ref.g_value is None:
                        cur_ref.g_value = self.count_g_value(cur_ref.matrix)
                    if cur_ref.f_value is None:
                        cur_ref.f_value = self.count_f_value(cur_ref)

                    if not cur_ref.c_nodes and is_first_leaf:
                        # запоминаем первый встреченный лист с минимальным f_value для последующего сравнения
                        node_with_min_f_value = cur_ref
                        # устанавливаем флаг проверки первого листа
                        is_first_leaf = not is_first_leaf

                    # если значение f текущего узла меньше или равно f записанного узла и
                    # если текущий узел - это лист
                    if (not cur_ref.c_nodes and
                        not is_first_leaf and
                        cur_ref.f_value <= node_with_min_f_value.f_value):
                        node_with_min_f_value = cur_ref # запоминаем ссылку на текущий узел

                    # если узел - это лист, выйти из цикла
                    if not cur_ref.c_nodes:
                        break

                    # помещаем ветви, ведущие налево, в память (стек)
                    # если только один дочерний узел, переходим к нему без работы с памятью
                    for i in range(len(cur_ref.c_nodes) - 1):
                        memory.append(cur_ref.c_nodes[i])

                    # переходим по ветви, ведущей направо
                    cur_ref = cur_ref.c_nodes[-1]