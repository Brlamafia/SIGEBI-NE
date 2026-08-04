-- EJERCICIO 10.3
SET SERVEROUTPUT ON
CREATE OR REPLACE PROCEDURE raise_comm
  (p_employee_id IN employees.employee_id%TYPE,
   p_tasa        IN NUMBER) IS
BEGIN
  UPDATE employees
     SET salary = salary * (1 + p_tasa / 100)
   WHERE employee_id = p_employee_id;
END;
/

CREATE OR REPLACE PROCEDURE employee_proc IS
  CURSOR c_it_prog IS
    SELECT employee_id, last_name, salary
      FROM employees
     WHERE job_id = 'IT_PROG'
     ORDER BY employee_id;
  v_tasa NUMBER;
  v_nuevo employees.salary%TYPE;
BEGIN
  FOR r_emp IN c_it_prog LOOP
    IF r_emp.salary BETWEEN 2600 AND 6000 THEN
      v_tasa := 8;
    ELSIF r_emp.salary BETWEEN 6001 AND 7000 THEN
      v_tasa := 6;
    ELSIF r_emp.salary BETWEEN 7001 AND 8000 THEN
      v_tasa := 4;
    ELSE
      v_tasa := 2;
    END IF;

    raise_comm(r_emp.employee_id, v_tasa);
    SELECT salary INTO v_nuevo FROM employees
     WHERE employee_id = r_emp.employee_id;

    DBMS_OUTPUT.PUT_LINE('--- Datos del empleado ---');
    DBMS_OUTPUT.PUT_LINE('*** Apellido: ' || r_emp.last_name);
    DBMS_OUTPUT.PUT_LINE('*** Aumento [%]: ' || v_tasa || '%');
    DBMS_OUTPUT.PUT_LINE('*** Salario [V]: ' ||
      TO_CHAR(r_emp.salary, 'fm$99,999.00'));
    DBMS_OUTPUT.PUT_LINE('*** Salario [N]: ' ||
      TO_CHAR(v_nuevo, 'fm$99,999.00'));
  END LOOP;
END;
/

EXECUTE employee_proc;
ROLLBACK;
