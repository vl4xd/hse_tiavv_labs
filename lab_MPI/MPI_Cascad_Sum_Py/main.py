from mpi4py import MPI

#test = []

def addCounter(counter1, counter2, datatype):

    for item in counter2:
        if item in counter1:
            #test.append(1)
            print('Summing: ', counter1[item], '+', counter2[item])
            counter1[item] += counter2[item]
        else:
            counter1[item] = counter2[item]
    return counter1

if __name__ == "__main__":

    comm = MPI.COMM_WORLD

    myDict = {'value':comm.rank}

    counterSumOp = MPI.Op.Create(addCounter, commute=True)

    totDict = comm.reduce(myDict, op=counterSumOp)
    print(comm.rank, totDict)

    MPI.Finalize()