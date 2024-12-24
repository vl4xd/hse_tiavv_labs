from mpi4py import MPI
import random

# Функция для непосредственного суммирования значений у процессов
def addCounter(counter1, counter2, datatype):
    for item in counter2:
        if item in counter1:
            counter1[item] += counter2[item]
        else:
            counter1[item] = counter2[item]    

    return counter1

# Функция для каскадного суммирования элементов массива
def cascade_summing():

    comm = MPI.COMM_WORLD
    random_num = random.randint(1, 100) # Генерируются случайные числа, от 1 до 100.
    myDict = {'Cascade sum' : random_num}#comm.rank}
    counterSumOp = MPI.Op.Create(addCounter, commute=True)
    totDict = comm.allreduce(myDict, op=counterSumOp) # Параллельное суммирование

    file = open(f'mpis/{comm.rank}.txt', 'w')
    file.write(f'Process {comm.rank} = {random_num}')
    file.close()

    print(totDict)


cascade_summing()

